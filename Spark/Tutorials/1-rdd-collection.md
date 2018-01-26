
# RDD basics

This notebook will introduce essential Spark operations, to work with data. The data is read into a distributed dataset, then information can be extracted by defining a (chain) of **transformation** function(s) that process the data and finally an **action** function that extracts the information.    

## Data containers

Spark has two main types of data containers (formally these are API's).

(1) an **RDD** or Resilient Distributed Dataset, which is an immutable distributed collection of elements of your data, partitioned across nodes in your cluster that can be operated in parallel with a low-level API that offers *transformations* and *actions*. Since they are immutable, every tranformation can be seen as an operation that generates a new RDD and action as an operation that generates a result. 

(2) a **Dataframe** is an immutable distributed collection of data. Unlike an RDD, data is organized into named columns, like a table in a relational database. Designed to make large data sets processing even easier, DataFrame allows developers to impose a structure onto a distributed collection of data, allowing higher-level abstraction

The first part of the tutorial focusses on RDDs.

We can create an RDD from memory using the `parallelize(collection)` on the SparkContext (usually abbreviated as `sc`).


```python
names = sc.parallelize(["Peter", "Mike", "James", "John", "Luke", "Phil", "Mike"])
```

You cannot directly view the contents of an RDD, rather you need an action to that. So inspection of `names` returns just type description.


```python
names
```




    ParallelCollectionRDD[0] at parallelize at PythonRDD.scala:480



### Actions

We can apply several **action**s to an RDD to get results from it. The `collect()` action simply returns all the elements of the RDD as a Python list.


```python
names.collect()
```




    ['Peter', 'Mike', 'James', 'John', 'Luke', 'Phil', 'Mike']



For inspection, the `first()` action return the first element as a value, and the `take(n)` action returns the first n elements as a List.


```python
names.first()
```




    'Peter'




```python
names.take(5)
```




    ['Peter', 'Mike', 'James', 'John', 'Luke']



## RDD with structured elements

We can also create an RDD in which every element is a collection. Commonly, tuples are used to assign multiple values to every element. Every position can have a diffent datatype, so we can create an element that stores the name and age of a person together. In this RDD, the structure of each element is a tuple consisting of a `(name, age)`.


```python
person = ('Peter', 30)
person
```




    ('Peter', 30)



 We can access elements in tuples through slicing and indexing. In this example, `[0]` is a person's name and `[1]` a person's age.


```python
person[1]
```




    30



So then we can create an RDD in which each element represents a person and stores the name and age in a `(name, age)` tuple.


```python
persons = sc.parallelize([('Peter', 30), ('Cindy', 12), ('Mike', 20), ('John', 25), ('Mary', 27)])
persons.collect()
```




    [('Peter', 30), ('Cindy', 12), ('Mike', 20), ('John', 25), ('Mary', 27)]



## Functions to transform elements

Spark allows us to easily and efficiently extract any result from an existing dataset. You should think of extracting some result as a sequence of **transformation**s. For instance, when we want to compute the average age we can first tranform `persons` into a dataset with just the ages, and take the `mean()` of those values. 

For each step in a transformation, we need to define a **function** that describes how the data is transformed. Commonly, these functions accept an element from the dataset en return a result according to their purpose. Let us start with a function that transforms a `(name, age)` element to just an `age`.

So a function that returns the age for a person whose data is structured as `(name, age)` would be:


```python
def person_to_age(person):
    return person[1]

person_to_age(person)
```




    30



However, writing many functions like that is rather verbose. For simple single line operations, we typically use **lambda functions** instead of defining a function for it. These lambdas behave just like any other function. Before the `:` are the arguments and after the `:` the result that is returned.


```python
p2a = lambda x: x[1]
p2a(person)
```




    30



### Map() to tranforms every element

The most basic transformation of an RDD is a map. The `map(f)` transformation is given a function f and calls that function `f(element)` on every element, and stores the results returned by those function calls as a new RDD. In the first example we transform the elements in the RDD from a `(name, age)` to just their `age`. 

We can make the transformation by either passing the previously defined functions `person_to_age` or `p2a`.


```python
ages = persons.map(person_to_age)
ages.collect()
```




    [30, 12, 20, 25, 27]



However, we will mostly type lambda expressions inline.


```python
ages = persons.map(lambda x: x[1])
ages.collect()
```




    [30, 12, 20, 25, 27]



### Chaining transformations

Think of each transformation (like `map()`) as a function that returns an RDD. On an RDD we can apply a new transformation or in the end an action. Thus we can write a result on a single like so:


```python
persons.map(lambda x: x[1]).collect()
```




    [30, 12, 20, 25, 27]



However, beware that you code remains readable. Note:
- Assigning the result of a single transformation to a variable does not make it slower
- A meaningful variable name you assign the result to can help understand the code

So to return the number of years these persons are older than youngest amongst them, we can simply retrieve the age of the youngest person, and use a new transformation in which we lower all ages with the youngest.


```python
youngest = persons.map( lambda x: x[1]).min()
persons.map(lambda x: x[1] - youngest).collect()
```




    [18, 0, 8, 13, 15]



To preserve the name of each element, we can have our function output a tuple `(name, years older than youngest)`.


```python
persons.map(lambda x: (x[0], x[1] - youngest) ).collect()
```




    [('Peter', 18), ('Cindy', 0), ('Mike', 8), ('John', 13), ('Mary', 15)]



### Filter() to keep only selected elements

By definition, a `map()` transformation will always map every element to exactly one result. So if the input RDD contains `n` elements the resulting RDD will also contain `n` elements. To leave out selected elements we can use the `filter()` transformation, wich evaluates a function for every element and only keeps the elements for which the function returns `True`. This function accepts a single element as a parameter and returns a boolean.


```python
adults = persons.filter(lambda x: x[1] >= 18)
adults.collect()
```




    [('Peter', 30), ('Mike', 20), ('John', 25), ('Mary', 27)]



## Caching

All transformations in Spark are **lazy**, in that they do not compute their results right away (a.k.a. Lazy Evaluation). Instead, they just remember the transformations applied to some base dataset (e.g. a file). The transformations are only computed when an action requires a result to be returned to the driver program. This design enables Spark to run more efficiently. For example, when only a small subset of the entire set is needed to produce the results (e.g. `take(5)`), there is no need to compute all results.

By default, intermediary results of transformations are computed and kept in memory as long as Spark senses they are still needed to complete the current action. This means that when an action finishes by default all intermediary results are removed, and have to be recomputed when needed again. However, you can manually instruct Spark to keep an RDD in memory, for much faster access the next time you reuse it. This is called caching or creating persistent RDDs. To cache an RDD to remain in memory you can use the `cache()` method. There is also support for persisting RDDs on disk.


```python
adults.getStorageLevel()
```




    StorageLevel(False, False, False, False, 1)




```python
adults.cache()
adults.getStorageLevel() # lists: disk, memory, offheap, deserialized, #replications
```




    StorageLevel(False, True, False, False, 1)




```python

```


```python

```
