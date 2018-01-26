
# Statistics

A standard task in large-scale data processing, is to compute some basic statistics from a dataset. In the tutorial part, we will experiment on small samples, for the purpose of transparency in the processing. In the exercises, we will tackle a large dataset to make processing a bit more realistic.


```python
data = sc.parallelize([('M', 25), 
                       ('M', 20), 
                       ('M', 30), 
                       ('F', 25),
                       ('F', 20),
                       ('M', 30)])
values = data.map(lambda x:x[1])
```

## Basic statistics

There are a few basic operations, such as **count**ing the number of elements, taking the **sum** and the **mean** (average) of values. Since the mean is equal to the sum divided by the number of elements, sum/count will also give the mean.


```python
data.count()
```




    6




```python
values.sum()
```




    150




```python
values.mean()
```




    25.0




```python
values.sum()/data.count()
```




    25.0



## Standard descriptive statistics

There are also standard functions on RDDs to compute the standard deviation (average difference to the mean) and to distribute the values over buckets to prepare the data to plot in a histogram. For the standard deviation there are two functions, when working with sampled data (i.e. if we cannot observe all cases), the **sampleStddev** is more appropriate. 


```python
values.sampleStdev()
```




    4.4721359549995796



To plot a histogram, the data is distributed over equally sized buckets, and for every bucket the frequency represents the number of values that fall into that bucket. When we distribute the data over two equally sized buckets, the first list gives the bucket boundaries (20-25) and (25-30), and the second list the frequencies, e.g. 2 values fall into the (20-25) bucket (for all buckets except the last the upper bound is exclusive).


```python
values.histogram(2)
```




    ([20, 25, 30], [2, 4])



## Aggregations

For aggregated counts over keys and values, you can use the **countByKey** action on (key, value) pairs, and the **countByValue** action over elements.


```python
data.countByKey()
```




    defaultdict(int, {'F': 2, 'M': 4})




```python
values.countByValue()
```




    defaultdict(int, {20: 2, 25: 2, 30: 2})



If you want to do a different computation over keys, then the  **reduceByKey**, **aggregateByKey**, **combineByKey** and **groupByKey** are the transformation function that allow you to do that. When the sum, min or max over values are required, reduceByKey offers the most straightforward solution. But for more complex function the other transformations provide all the tools you need. 


```python
results = data.reduceByKey(lambda x, y: max(x, y))
print(results.collect())
```

    [('M', 30), ('F', 25)]



```python

```
