
# RDD from file

Instead of creating RDD's from lists, we will most often read the data from files. In this tutorial, we use a .csv file that contains how often babies received a given name

### Downloading data

For this tutorial there may already be a symbolic link pointing to the file, but the download code is retained as a reference on how to download data. Pythons urllib can be used to download a URL and store the downloaded file on disk. We first check whether the file exists and if it does not the file is downloaded. You may check if it works by deleting the babynames.csv file and running the cell below.


```python
if not os.path.exists("babynames.csv"):
    import urllib.request
    f = urllib.request.urlretrieve ("https://health.data.ny.gov/api/views/jxy9-yhdk/rows.csv?accessType=DOWNLOAD", \
                                    "babynames.csv")
```

### Reading an RDD from file

There are a few options to read textfiles in Spark. The first is using the `textFile()` method of the SparkContext (abbreviated as `sc`). 

We can view a sample from the RDD with the `take(n)` action, which shows the first n elements. `TextFile()` simply uses every line in the file as a string element. 


```python
babyrddprimitive = sc.textFile("babynames.csv")
babyrddprimitive.take(5)
```




    ['Year,First Name,County,Sex,Count',
     '2013,GAVIN,ST LAWRENCE,M,9',
     '2013,LEVI,ST LAWRENCE,M,9',
     '2013,LOGAN,NEW YORK,M,44',
     '2013,HUDSON,NEW YORK,M,49']



We can use a transformation to remove the first line. The action `first()` returns the first element from the RDD, which is the line with the header. The transformation `filter(condition)` evaluates the condition for every element and only keeps the elements for which the condition is true. For the *condition*, we pass a function that accepts a single element as a parameter and returns a boolean. This can be a regular Python function that is described by a `def` but we will often use **`lambda` function**s, which is a short way to describe an anonymous function with the parameter being the part before the colon, and the result the part after the colon.


```python
firstline = babyrddprimitive.first()
babyrddnofirstline = babyrddprimitive.filter(lambda x: x != firstline)
babyrddnofirstline.take(5)
```




    ['2013,GAVIN,ST LAWRENCE,M,9',
     '2013,LEVI,ST LAWRENCE,M,9',
     '2013,LOGAN,NEW YORK,M,44',
     '2013,HUDSON,NEW YORK,M,49',
     '2013,GABRIEL,NEW YORK,M,50']



Then to transform every element into a list of column values, we can just use the python `split()` function, using a `','` as the character to split every string. Let's first inspect what split does on a single line of text:


```python
'2013,GAVIN,ST LAWRENCE,M,9'.split(',')
```




    ['2013', 'GAVIN', 'ST LAWRENCE', 'M', '9']



Notice that split returns a List rather than a tuple, but that is ok, lists behave the same in Python. We can use a `map` transformation to transform every element into a list of values.


```python
babyrdd = babyrddnofirstline.map(lambda x: x.split(','))
babyrdd.take(5)
```




    [['2013', 'GAVIN', 'ST LAWRENCE', 'M', '9'],
     ['2013', 'LEVI', 'ST LAWRENCE', 'M', '9'],
     ['2013', 'LOGAN', 'NEW YORK', 'M', '44'],
     ['2013', 'HUDSON', 'NEW YORK', 'M', '49'],
     ['2013', 'GABRIEL', 'NEW YORK', 'M', '50']]



After we have split the lines, the RDD is transformed into a structure that is more easily processed. For instance, we can simply extract the names from the RDD using:


```python
babyrdd.map(lambda x: x[1]).take(5)
```




    ['GAVIN', 'LEVI', 'LOGAN', 'HUDSON', 'GABRIEL']



### Saving an RDD to a textfile

The `saveAsTextFile(directory)` action, stores the RRD in the given directory. The data is often stored in numbered parts, which when distributed across disks actually improves disk throughput when reading in. When an RDD is saved, every element is just converted to a string. Since our elements are lists, a string representation is not easy to read back in. Therefore is better to transform every element to a comma seperated values string. We can do that very easily with Python's `join()` function. Consider the difference, the join function formats it in a True .csv format.


```python
babyrdd.first()
```




    ['2013', 'GAVIN', 'ST LAWRENCE', 'M', '9']




```python
','.join(babyrdd.first())
```




    '2013,GAVIN,ST LAWRENCE,M,9'



Spark cannot write data into existing folders, so if we want to repeat the save code we must delete the output folder. The code below does that.


```python
import os, shutil
if os.path.isdir('myoutput'): # must remove if exists
    shutil.rmtree('myoutput')
```


```python
babyrdd.map(lambda x: ','.join(x)).saveAsTextFile('myoutput')
```

And we can view the written file. There are probably 2 data files part-00000 and part-00001 in the folder. By default, small datasets are split into 2 parts for parallel processing by two cores. It is saved in that way to allow spark to load the dataset faster when reloaded. If the size of the data increases, Spark should use more parallel processes. If it does not, you can increase the number of parallel processes manually.


```python
%%bash
head myoutput/part-00000
```

    2013,GAVIN,ST LAWRENCE,M,9
    2013,LEVI,ST LAWRENCE,M,9
    2013,LOGAN,NEW YORK,M,44
    2013,HUDSON,NEW YORK,M,49
    2013,GABRIEL,NEW YORK,M,50
    2013,THEODORE,NEW YORK,M,51
    2013,ELIZA,KINGS,F,16
    2013,MADELEINE,KINGS,F,16
    2013,ZARA,KINGS,F,16
    2013,DAISY,KINGS,F,16



```python

```


```python

```
