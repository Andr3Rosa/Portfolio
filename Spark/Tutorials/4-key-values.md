
# Key-Values

## Custom function to read a flat text file into Spark

First, lets setup an RDD to process the babyname data. We can use what we have learned previously to create a function that sets up an RDD for a multi-column flat text file.


```python
import os
filename = 'babynames.csv'
if not os.path.exists(filename):
    import urllib.request
    urllib.request.urlretrieve ("https://health.data.ny.gov/api/views/jxy9-yhdk/rows.csv?accessType=DOWNLOAD", \
                                filename)
```


```python
def readCSV(fname, header=False, separator=','):
    rdd = sc.textFile(fname)
    if header:
        firstline = rdd.first()
        rdd = rdd.filter(lambda x: x != firstline)
    return rdd.map(lambda x: x.split(separator))

babyrdd = readCSV(filename, header=True)
```


```python
babyrdd.first()
```

## Key-Value pairs

The most common **flow pattern** for distributed processing is the **Map-Reduce** pattern. (1) All elements in a dataset are first processed by a **map(e -> (k, v))** function, which consumes elements and results in **(key, value)** pairs. By protocol, the key-values identify which values go together, i.e. pairs that have the same key-value are considered values for the same key. (2) The (key, value) pairs that are produced by the map() function, are grouped by their key-values (often this is an implicit step, we do not have to specifically write instructions for this). (3) Per key-value, a **reduce( v1, v2 -> v )** function is called on two values `v1` and `v2`, and produces a single value `v` of the same type.

## ReduceByKey

To compute the counts per gender in one flow, we wish to transform every element in the RDD into a (gender, count) pair. Initially we can simply transform every element into (gender, 1) and then sum the values per gender. The transformation **reduceByKey( v1, v2 -> v )** consumes two values `v1` and `v2` that belong to the same key and reduces it to a single value 'v' of the same type. Although reduce is an action, reduceByKey is a tranformation that results in a new RDD. We can use the **collect()** action to collect all results.

Note: the values are unordered, and therefore the function must be *commutative*, i.e. produce the same result regardless of the order of the operands.


```python
kv = babyrdd.map(lambda x: (x[3], int(x[4])))
kv.take(5)
```


```python
kv.reduceByKey(lambda x, y: x + y).collect()
```

## CollectAsMap

When working with (key, value) pairs, it can be easier to retrieve the results as a Python dict object. In Scala (the langauge in which Spark was written), the equivalent of a dict is a Map, hence the action name **collectAsMap()** to collect the results as a dictionary of key-values.


```python
d = kv.reduceByKey(lambda x, y: x + y).collectAsMap()
(d['M'], d['F'])
```

## Lookup

We can also lookup which values are associated with a single key, by using the **lookup(key)** action. Lookup will return a list of values (even if there is None or one), beacuse keys may not exist and keys are not required to be unique.


```python
kv.reduceByKey(lambda x, y: x + y).lookup('M')
```

## AggregateByKey

For more control, we can use the **aggregateByKey(zero() -> r, f1(r, v) -> r, f2(r1, r2) -> r)**. In contrast to reduceByKey, the aggregation transformation (a.k.a. fold) accumulates a result that can be of a different type than the values. Initially, the **zero()** function is called to generate an initial accumulator (often zero or empty). Then for every value, f1 is called to add a value to the accumulator. Note that because we are working with distributed data, multiple aggregators will work in parallel each producing an accumulator as its reults. To merge these partial results of two aggregators, a final function f2 is called to merge two accumulators.

In the example below, we initialize the count per gender as 0. Then for every (key, value) pair, the second function adds the value to the accumulator. Finally, if there are multiple nodes that are used in parallel on the same key, their results can be added using the final function. 


```python
kv.aggregateByKey(\
                  0, # initial value for an accumulator \
                  lambda r, v: r + v, # function that adds a value to an accumulator \
                  lambda r1, r2: r1 + r2 # function that merges/combines two accumulators \
                 ).collect()
```

To count the number of distinctly different names, we can use aggregateByKey to have more control over the reduce function. First we will look at Python sets to see how we can work with sets to obtain a proper count. We can create a set using the **set()** constructor, and add elements using **add()**. By definition, a set contains only unique elements, thus adding a value that already exists results in no change. 


```python
s = set(['Peter'])
s.add('Mike')
s.add('Peter')
print(s, 'size:', len(s))
```

We can union two sets using the **union()** method, resulting in a set that contains all elements in either set.


```python
t = set(['Peter', 'James'])
u = s.union(t)
print(u, 'size:', len(u))
```

We can then accumulate the names in a set to find the number of unique names per gender. First, we need to setup an RDD that contains (gender, name) pairs. 


```python
gendernames = babyrdd.map(lambda x: (x[3], x[1]))
gendernames.take(5)
```

Note that since the *add* function does not return a value, we need a function to first add a name and then return the set of names.


```python
def addToSet(names, name):
    names.add(name)
    return names

r = gendernames.aggregateByKey(\
                               set(), # initial value for an accumulator \
                               addToSet, # function to add a value to an accumulator \
                               lambda r1, r2: r1.union(r2) # function to merge two accumulators \
                              )
r.take(1)
```

Take a good look at the structure of the RDD. The contents of the RDD is printed within []. Evert element of the RDD is a tuple (in this case a key-value pair), which is printed within (). Within the key-value pairs for the keys 'M' and 'F', the values are sets which are printed with {}. Note that in many languages (e.g. Python, Java), a set is just a dictionary without values. 

Since every element x is a (key, value) pair, we can address the key as x[0] and the value as x[1]. Then we can count the number of unique names per gender as the length of their sets of names.


```python
r.map(lambda x: (x[0], len(x[1]))).collect()
```

## CombineByKey

The **CombineByKey( init(v) -> acc, f1(acc, v) -> acc, f2(acc, acc) -> acc )** tranformation function is similar to aggregateByKey, except that instead of a zero function an initializer function **init** is used on the first value to setup an accumulator.


```python
gendernames.combineByKey(\
                               lambda x: set([x]), # initial value for an accumulator \
                               addToSet, # function to add a value to an accumulator \
                               lambda r1, r2: r1.union(r2) # function to merge two accumulators \
                              ).map(lambda x: (x[0], len(x[1]))).collect()
```

## groupByKey

The **groupByKey()** transformation groups all values for the same key into a list.


```python
namesByGender = babyrdd.map(lambda x: ((x[1], x[3]), int(x[4])) )
namesByGender.take(5)
```


```python
namesByGender.groupByKey().take(2)
```


```python
namesByGender.groupByKey().mapValues(sum).take(5)
```

The result are iterables, which are evaluated when needed. In this example, the Python function `Counter` counts how often every element appears in the collection.


```python
from collections import Counter
gendernames.groupByKey().mapValues(lambda x: Counter(x)).take(1)
```

## SubtractByKey

From an RDD of (key, value) elements we can remove the keys that exist in another RDD using the **subtractByKey(r)** transformation. This transformation is equivalent to the *not in* operator in SQL.

First, construct an RDD with (name, gender) pairs


```python
nameGenders = babyrdd.map(lambda x: (x[1], x[3]))
nameGenders.take(2)
```

Then group the pairs by name, count the number of different genders by the length of a set


```python
nameCountGenders = nameGenders.groupByKey().mapValues(lambda x: len(set(x)))
nameCountGenders.take(2)
```

And keep only names that are used for one gender


```python
nameSingleGender = nameCountGenders.filter(lambda x: x[1] < 2)
nameSingleGender.take(2)
```

Now show only the names that do are not used for a single gender.


```python
nameGenders.subtractByKey(nameSingleGender).keys().distinct().take(8)
```

## Keys and Values

From an RDD of (key, value) elements, we can use only the keys using the **keys()** transformation and use only the values using the **values()** transformation.


```python
gendernames.keys().take(10)
```


```python
gendernames.values().take(10)
```
