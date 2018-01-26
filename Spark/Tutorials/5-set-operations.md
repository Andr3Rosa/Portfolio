
# Set operations on RDDs

Spark support several operations from set theorie, such as **union**, **subtract**, **intersect**. However, different from mathmetics and other programming languages, spark does not garantee unique elements, nor check for duplicates unless the **distinct()** transformation is used. 


```python
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

## `Subtract`

RDD's also support some set operations, such as subtract and union. The set operation subtract `A - B` contains all elements in `A` except those that are in `B`. **subtract** needs elements that are comparable, or when working with (key, value) pairs **subtractByKey** needs a key that is comparable. In this example we will generate a comparable key by using the str function on each element. The end result contains records of female names or male names before `'K'`. After the subtract, we map every element `x` back to its value `x[1]` only, which is the original record in babyrdd.

We can combine filters in one function using Pythons *and* operator. Every element x is a row, and the first name x[1] is a string. In Python strings can simply be addressed as lists of characters, therefore x[1][0] corresponds to the first letter of the first name of an element x. The comparison to 'L' filters out first names up to 'L'.


```python
male_first_half = babyrdd.filter(lambda x: x[3] == 'M' and x[1][0] > 'L')
male_first_half.take(5)
```




    [['2013', 'THEODORE', 'NEW YORK', 'M', '51'],
     ['2013', 'MASON', 'ST LAWRENCE', 'M', '8'],
     ['2013', 'NOAH', 'ST LAWRENCE', 'M', '8'],
     ['2013', 'SEBASTIAN', 'NEW YORK', 'M', '57'],
     ['2013', 'SAMUEL', 'NEW YORK', 'M', '59']]




```python
babykv = babyrdd.map(lambda x: (str(x), x))
males = male_first_half.map(lambda x: (str(x), x))
f = babykv.subtractByKey(males)
f.map(lambda x: x[1]).take(5)
```




    [['2013', 'HUDSON', 'NEW YORK', 'M', '49'],
     ['2013', 'BRANDON', 'SUFFOLK', 'M', '50'],
     ['2013', 'SABRINA', 'KINGS', 'F', '15'],
     ['2013', 'NYLAH', 'KINGS', 'F', '15'],
     ['2013', 'HELEN', 'KINGS', 'F', '15']]



## Union

In Spark, the **union** transformation can be used to combine two RDD's into one. In contrast to subtract, in Spark **union** is not a true set operatation in the sense that it does not remove duplicates, therefore `a.union(b).count() == a.count() + b.count()`. Union does not care about the types or structure of elements, the new RDD contains all elements even if they have different types.


```python
f = babykv.union(babykv)
print(babykv.count(), f.count())
```

    145570 291140


## Intersection

The **intersection** tranformation between two RDDs results in an RDD that contains only the elements that appear in both. To use intersect, the entire elements must be hashable and comparable.


```python
males = babyrdd.filter(lambda x: x[3] == 'M').map(lambda x: x[1])
newyork = babyrdd.filter(lambda x: x[2] == 'NEW YORK').map(lambda x: x[1])
males.intersection(newyork).take(5)
```




    ['LOGAN', 'JONATHAN', 'JACKSON', 'SAMUEL', 'JAYDEN']




```python

```
