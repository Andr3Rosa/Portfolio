
## Effiency

In the map/reduce framework, a `map` is a very cheap operation, because it is executed in parallel on local data. A `reduce` is a very expensive operation, because all data has to be shuffle sorted and re-partitioned over reducers. For efficient processing, a few rules of thumb are: filter early, and avoid shuffle sort (reduce transformations like join, groupByKey).

One way to avoid expensive shuffle sorts, is to replace `join` operations with `broadcast joins`. For large datasets, **the speedup can easily exceed 100x-1000x**, so the correct use of broadcast joins is vital for efficient processing.

## Broadcast ##

Broadcasting is the sending a set of data to each worker. When one table is small, it is much more efficient to join tables using a broadcast than to use a join operator. The first example shows the mechanism of broadcasting by turning one table into a Python dictionary, and then mapping the other table with a function that uses that dictionary. The additional cost is that the dictionary has to be send (*broadcasted*) to all workers, the gain is that the map can operate locally on the data and no reduce is required. Processing will be more efficient when the gain of broadcasting is greater than the cost.


```python
productdata = """
Apple	Fruit
Pear	Fruit
Pizza	FastFood
Fries	FastFood
"""

salesdata = """
Apple	2000
Apple	1000
Pizza	3000
Fries	5000
"""
```


```python
products = sc.parallelize(productdata.strip().split('\n'))
products = products.map(lambda x: tuple(x.split()))
```


```python
products.collect()
```




    [('Apple', 'Fruit'),
     ('Pear', 'Fruit'),
     ('Pizza', 'FastFood'),
     ('Fries', 'FastFood')]




```python
sales = sc.parallelize(salesdata.strip().split('\n'))
sales = sales.map(lambda x: tuple(x.split()))
sales = sales.map(lambda x: (x[0], int(x[1])))
sales.collect()
```




    [('Apple', 2000), ('Apple', 1000), ('Pizza', 3000), ('Fries', 5000)]



## Broadcast Filtering ##

First, we will use a *broadcast* as a lookup, much like an SQL subselect. In this case we want the sales records for all 'Fruit'.


```python
fruit = set( products.filter(lambda x: x[1] == 'Fruit').\
             map(lambda x: x[0]).collect() )
fruit
```




    {'Apple', 'Pear'}



We queried for all products in the category 'Fruit' and obtained a set of keys that meet the requirements. We can then use the set of keys `fruit` to filter the `sales` RDD.


```python
sales.filter(lambda x: x[0] in fruit).collect()
```




    [('Apple', 2000), ('Apple', 1000)]



## Broadcast Join ##

Similarly, we can broadcast a dictionary to allow to append columns based on a key.


```python
fruit = products.collectAsMap()
fruit
```




    {'Apple': 'Fruit', 'Fries': 'FastFood', 'Pear': 'Fruit', 'Pizza': 'FastFood'}




```python
sales.map(lambda x: (x[0], x[1], fruit[x[0]])).collect()
```




    [('Apple', 2000, 'Fruit'),
     ('Apple', 1000, 'Fruit'),
     ('Pizza', 3000, 'FastFood'),
     ('Fries', 5000, 'FastFood')]



## Spark broadcast ##

We can improve the broadcast by using Spark's broadcast function. The 'subtle' difference is that the data is send once to every node, instead of along with every task. Suppose we have a cluster of 100 nodes each with 8 cores (800 cores in total), and we partitioned the data in 2000 parts. Using Spark's broadcast will send the variable 100 times, while not using broadcast will send the data 2000 times.

To use Spark broadcast, we need a dictionary (confusingly, also referred to as a map) which we can collect with a single statement using `collectAsMap` (alternatively, you can use `dict(foo.collect())`). The broadcast results in a `broadcast variable` that you can use in a function to do the lookup.


```python
fruit_bc = sc.broadcast(products.collectAsMap())
sales.map(lambda x: (x[0], x[1], fruit_bc.value[x[0]])).collect()
```




    [('Apple', 2000, 'Fruit'),
     ('Apple', 1000, 'Fruit'),
     ('Pizza', 3000, 'FastFood'),
     ('Fries', 5000, 'FastFood')]




```python

```
