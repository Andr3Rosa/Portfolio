
# Actions


Unlike **transformations** which produce RDDs, **action** functions produce a value back to the caller. In Spark, transformations are *lazy* evaluated, also known as a call-by-need evaluation strategy that delays the evaluation of an expression until its value is needed. **Lazy evaluation** allows for more efficient processing, avoiding processing of experessions that are not needed for the requested value, optimally combining multiple transformations, and sharing intermediate results when possible. As a rule, transformations describe a lazy process flow, and an **action** may trigger (part) of the flow to be executed.

In previous sections we have already learned the actions `collect()`, `take(n)`, `count()`, `sum()`, `mean()`, `min()` and `max()`. In this section we will focus on more advanced actions.


```python
names = sc.parallelize(["Peter", "Mike", "James", "John", "Luke", "Phil", "Mike"])
ages = sc.parallelize([20, 30, 25, 12, 18, 33])
```

## Get statistics with Sum, Count, Mean, Min, Max

Spark has several actions to compute simple statistics. `count()` returns the number of items in an RDD and `sum()` adds all elements in an RDD that contains numeric elements. 


```python
ages.count()
```




    6




```python
ages.sum()
```




    138



Similarly, `mean()` will return the average, `min()` the lowest value() and `max()` the highest value.


```python
ages.mean()
```




    23.0




```python
ages.min()
```




    12




```python
ages.max()
```




    33



## Top ##

Similar to what `take(n)` does, `top(n, f)` retrieves the top n elements according to a given function f. For this `f` must return a value that is *Comparable* (think of this as a datatype for which there exists greater-than and lower-than operators).

First, let's setup an RDD on the babynames collection, like in the previous tutorial (if the file babynames.csv does not exists, you need to download it using the previous notebook).


```python
babyrddprimitive = sc.textFile("babynames.csv")
firstline = babyrddprimitive.first()
babyrddnofirstline = babyrddprimitive.filter(lambda x: x != firstline)
babyrdd = babyrddnofirstline.map(lambda x: x.split(','))
```

The use `top` to find the 5 elements with the highest value. Note that in the example below we did not group the data by first name yet, so the results do not provide the most common babynames in Queens in 2013.


```python
baby2013queens = babyrdd.filter(lambda x: x[0] == '2013' and x[2] == 'QUEENS')
baby2013queens.top(5, lambda x: int(x[4]))
```




    [['2013', 'JAYDEN', 'QUEENS', 'M', '219'],
     ['2013', 'ETHAN', 'QUEENS', 'M', '216'],
     ['2013', 'SOPHIA', 'QUEENS', 'F', '204'],
     ['2013', 'DANIEL', 'QUEENS', 'M', '203'],
     ['2013', 'ISABELLA', 'QUEENS', 'F', '192']]



# Reduce

The `reduce(f)` action can be used with a custom function f to aggregate values. The function f must accept two values and produce one value of the same datatype. In this example we use a max function, which returns the highest of two given values. Reduce will apply this function *recursively* until there is only one value left. When applied to the above RDD, we will see that this results in 219 which is the top count.


```python
counts2013queens = baby2013queens.map(lambda x: int(x[4]))    # we need int() because the count is still a text
counts2013queens.reduce(lambda x, y: max(x, y))
```




    219



We will later on see more advanced aggregation functions.


```python

```
