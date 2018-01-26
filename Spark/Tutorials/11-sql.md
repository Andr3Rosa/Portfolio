
# Spark SQL and Data Frames

It is very easy to express data queries when used together with the SQL language. Moreover, Spark distributes this column-based data structure transparently, in order to make the querying process as efficient as possible.      


```python
filename = '2008.csv.bz2'
if not os.path.exists(filename):
    import urllib.request
    urllib.request.urlretrieve ("http://stat-computing.org/dataexpo/2009/2008.csv.bz2", \
                                filename)
```


```python
f = sqlContext.read.format("com.databricks.spark.csv").\
    options(header="true", inferSchema = "true").load(filename)
```


Once we have our RDD of `Row` we can infer and register the schema.  


```python
f.registerTempTable("flights")
```

Now we can run SQL queries over our data frame that has been registered as a table. Note that when using SQL, there is no need to cast DepDelay to numeric, since SQL converts Strings to numeric when applying avg().


```python
numflights = sqlContext.sql("""
    SELECT FlightNum, count(*), avg(DepDelay) as depdelay
    FROM flights GROUP BY FlightNum
""")
numflights.show(5)
```

    +---------+--------+------------------+
    |FlightNum|count(1)|          depdelay|
    +---------+--------+------------------+
    |     1580|    2774| 5.152813067150635|
    |      463|    2302|14.447111111111111|
    |     2366|    1142|11.769848349687779|
    |      496|    1946| 9.009926854754442|
    |     3918|     618|10.884176182707993|
    +---------+--------+------------------+
    only showing top 5 rows
    


The results of SQL queries are DataFrames which can be processed further using the DataFrame API.  
