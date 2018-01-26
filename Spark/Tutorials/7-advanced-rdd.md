
# Advanced RDD functions

For processing (key, value) RDDs, there are a few functions that simplify common flow patterns. 

## MapValues

In contrast to the map() transformation, **mapValues( f( v -> v' ) )** can only be used on (key, value) pairs. It applies a function f to map a value v to v', and results in (key, v') pairs.


```python
numbers = sc.parallelize([('John' ,3), ('Peter' ,8), 
                          ('Peter', 10), ('Mike', 4), ('Mike', 7)])
```


```python
numbers.mapValues(lambda x: x + 1).collect()
```




    [('John', 4), ('Peter', 9), ('Peter', 11), ('Mike', 5), ('Mike', 8)]



## CountByKey ##

Complementary to the `count()` actions, that counts the elements in an RDD, `countByKey()` counts the elements per key. There is no need to group the elements by key, but the elements do need to be structured as (key, value).


```python
numbers.countByKey()
```




    defaultdict(int, {'John': 1, 'Mike': 2, 'Peter': 2})



## Flattening

When mapping an RDD of N-elements, this will always result in an RDD of exactly N-elements, since every element will be transformed into exactly one value. When we want to map an element to zero or more elements, the default approach is starts with mapping every result to a list, lists that can contain zero or more elements. An RDD of lists can be **flatten**ed to an RDD of the elements that are contained in the lists, using the **flatten** transformation function. 

Consider the case where we want to process all the words in a textfile. Initially, the *textfile()* method will create an element for every line. Some lines are empty (contain no words), but most lines contain multiple words. To transform the lines to words we can first map each line to a list of words using Python's **split()** function, and then flatten the result to obtain an RDD of words.

Let us first consider a small in-memory example.


```python
lines = sc.parallelize(["This is line 1", "", 
                        "This is line two", "Three", 
                        "This is the last line"])
lists = lines.map(lambda x: x.split())
print(lists.count(), lists.collect())
```

    5 [['This', 'is', 'line', '1'], [], ['This', 'is', 'line', 'two'], ['Three'], ['This', 'is', 'the', 'last', 'line']]


The RDD lists contains 5 elements, 5 lists of 0 or more words. To flatten the RDD, you will notice that Spark actually has no **flatten** transformation, but there is a **flatMap()** transformation instead. Presumably, since flattening is a relatively expensive operation and combining a flatten with a map relatively inexpensive, the absence of a flatten transformation may make programmers more conscious about always combining a map with a flatten. In any case, we can use **flatMap(lambda x:x)** with a so called identity function, to just flatten. 


```python
words = lists.flatMap(lambda x:x)
print(words.count(), words.collect())
```

    14 ['This', 'is', 'line', '1', 'This', 'is', 'line', 'two', 'Three', 'This', 'is', 'the', 'last', 'line']


For educational purposes, it is good to see how the lines are mapped to lists and then how the RDD is flattened. However, this flow is commonly written in one operation.


```python
words2 = lines.flatMap(lambda x: x.split())
print(words2.count(), words2.collect())
```

    14 ['This', 'is', 'line', '1', 'This', 'is', 'line', 'two', 'Three', 'This', 'is', 'the', 'last', 'line']


It is important to realize that flatten will only remove **one nested level**. To show what happens, we force a scenario in which an RDD in which the elements are lists with both words and nested lists in them. 


```python
foolists = lines.map(lambda x: ['Foo', x.split()])
print(foolists.count(), foolists.collect())
```

    5 [['Foo', ['This', 'is', 'line', '1']], ['Foo', []], ['Foo', ['This', 'is', 'line', 'two']], ['Foo', ['Three']], ['Foo', ['This', 'is', 'the', 'last', 'line']]]


Observe carefully, the most outer [] represent the RDD. Within the RDD, the first element is `['Foo', ['This', 'is', 'line', '1']]`, which contains a word and a list. Flattening the result will remove the list surrounding the element, so that two new elements are the result: `'Foo'` and `['This', 'is', 'line', '1']`. The empty list that is embedded in another list does not simply vanish. Therefore the result is 10 elements, 2 elements for every line. Note that mixtures of lists and non-lists are rarely used in Spark, because it makes processing more complex.


```python
foowords = foolists.flatMap(lambda x:x)
print(foowords.count(), foowords.collect())
```

    10 ['Foo', ['This', 'is', 'line', '1'], 'Foo', [], 'Foo', ['This', 'is', 'line', 'two'], 'Foo', ['Three'], 'Foo', ['This', 'is', 'the', 'last', 'line']]


## flatMapValues

There is also a combined transformation **flatMapValues(f)** which is equivalent to a `mapValues(f)` followed by `mapFlat(i)` where i is the identify function. Flattening of (key, value) pairs causes the mapped values to remain bound to the original keys.


```python
kv = sc.parallelize([(1, "This is line 1"), 
                     (2, ""), 
                     (3, "This is line two"),
                     (4, "Three"), 
                     (5, "This is the last line") ])
letters = kv.flatMapValues(lambda x: x.split())
print(letters.count(), letters.collect())
```

    14 [(1, 'This'), (1, 'is'), (1, 'line'), (1, '1'), (3, 'This'), (3, 'is'), (3, 'line'), (3, 'two'), (4, 'Three'), (5, 'This'), (5, 'is'), (5, 'the'), (5, 'last'), (5, 'line')]


## Viewing the processing pipeline ##

For the evaluation of an RDD (and execution of the processing pipeline that results in the RDD), Spark generates a graph with the order in which functions are executed (called the DAG). You can view the DAG with the `toDebugString` on an RDD. These Directed Acyclic Graphs read backwards, so the last line in the output starts with the parallelization of the list from Python, which results in a PythonRDD, is converted to a PairwiseRDD, etc. The ShuffledRDD is inserted by Spark, as a requirement to perform the groupByKey.


```python
dag = numbers.map(lambda x: (x[1], 1))\
        .groupByKey().mapValues(sum).toDebugString()
str(dag, 'utf-8').split('\n')
```




    ['(48) PythonRDD[21] at RDD at PythonRDD.scala:48 []',
     ' |   MapPartitionsRDD[20] at mapPartitions at PythonRDD.scala:427 []',
     ' |   ShuffledRDD[19] at partitionBy at NativeMethodAccessorImpl.java:0 []',
     ' +-(48) PairwiseRDD[18] at groupByKey at <ipython-input-10-189bd755fc52>:1 []',
     '    |   PythonRDD[17] at groupByKey at <ipython-input-10-189bd755fc52>:1 []',
     '    |   ParallelCollectionRDD[0] at parallelize at PythonRDD.scala:480 []']




```python

```


```python

```
