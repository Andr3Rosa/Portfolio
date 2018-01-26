
# DataFrame

Spark 1.0 provided RDDs as its sole data API, supporting transformations and actions that can be used to control how the data is processed in a pipeline. The main advantage of RDDs is that they are simple and well understood because they deal with concrete classes, providing a familiar object-oriented programming style. 

The main disadvantage to RDDs is that they don’t perform particularly well. RDDs serialize all individual object which causes overhead. Other disadvantages are that the code that uses RDD can become less clear in the absense of concrete labels for columns, the use of RDDs demands lower level coding compared to more declarative languages (e.g. SQL), and external libraries rarely provide direct support for RDDs.

In Spark 1.3 DataFrames are supported, which allow for faster processing by allowing objects to be kept in memory instead of serializing everything to disk for every step. DataFrames are the data structure that is used by the very well supported `pandas` library, allowing to use external libraries that support pandas DataFrame in Spark. DataFrames provide a more table-like perspective on data, that can be operated with its own API or declarative languages such as SQL. The engine translates SQL queries to DataFrame operations, therefore, most SQL queries are as efficient as their DataFrame equivalent. 


```python
data = sc.parallelize([('M', 25), 
                       ('M', 20), 
                       ('M', 30), 
                       ('F', 25),
                       ('F', 20),
                       ('M', 30)])
```

Creating a DataFrame requires a dataset (e.g. RDD, List), and to provide a schema. In the most simple case, Spark will infer the correct datatypes from the dataset, so only the names for the columns are required.

Alternatively, data can also be read directly into DataFrames, for instance by using `pandas` or `parquet`.


```python
df = spark.createDataFrame(data, ['gender', 'age'])
print("The structure of the dataframe is {}".format(df))
#show the result of the dataframe
df.show()
print("the number of rows in the dataframe is {}".format(df.count()))
```

    The structure of the dataframe is DataFrame[gender: string, age: bigint]
    +------+---+
    |gender|age|
    +------+---+
    |     M| 25|
    |     M| 20|
    |     M| 30|
    |     F| 25|
    |     F| 20|
    |     M| 30|
    +------+---+
    
    the number of rows in the dataframe is 6


Different from RDDs, the columns are labelled which allows for clear code.


```python
df.filter(df.gender == 'M').show()
```

    +------+---+
    |gender|age|
    +------+---+
    |     M| 25|
    |     M| 20|
    |     M| 30|
    |     M| 30|
    +------+---+
    



```python
df.groupBy(df.gender).count().show()
```

    +------+-----+
    |gender|count|
    +------+-----+
    |     F|    2|
    |     M|    4|
    +------+-----+
    



```python
df.groupBy(df.gender).max("age").show()
```

    +------+--------+
    |gender|max(age)|
    +------+--------+
    |     F|      25|
    |     M|      30|
    +------+--------+
    



```python
from pyspark.sql.functions import *
df1 = df.groupBy(df.gender)
df1.agg(sum("age").alias("total"), min("age"), max("age")).show()
```

    +------+-----+--------+--------+
    |gender|total|min(age)|max(age)|
    +------+-----+--------+--------+
    |     F|   45|      20|      25|
    |     M|  105|      20|      30|
    +------+-----+--------+--------+
    


### Reading a .csv file into a DataFrame

For some file formats (e.g. .csv), there are also readers that can read the data directly as DataFrame. In this example, we will use a dataset with airline on-time statistics and delay causes. 


```python
filename = '2008.csv.bz2'
if not os.path.exists(filename):
    import urllib.request
    urllib.request.urlretrieve ("http://stat-computing.org/dataexpo/2009/2008.csv.bz2", \
                                filename)
```

### DataFrame schema

A DataFrame has a schema that consists of the column names and data types (and some optional information like if Null is allowed). In a .csv file, the column names are usually given by a header in the file, but the data type is usually not given. We will use three ways to assign a DataFrame to a schema.

### (a) Infer schema

The easiest option is to use the spark.csv reader. It can use the column labels that are in the file, and has an inferSchema option that infers the datatypes from the data. However, inferSchema has two disadvantages: (1) it is a very expensive operation for large data volumes (will go over the entire dataset), (2) it is not perfect, often datatypes are set to String or Object when there are missing values.


```python
f = sqlContext.read.format("com.databricks.spark.csv").\
    options(header="true", inferSchema = "true", ).load(filename)
f.printSchema()
```

    root
     |-- Year: integer (nullable = true)
     |-- Month: integer (nullable = true)
     |-- DayofMonth: integer (nullable = true)
     |-- DayOfWeek: integer (nullable = true)
     |-- DepTime: string (nullable = true)
     |-- CRSDepTime: integer (nullable = true)
     |-- ArrTime: string (nullable = true)
     |-- CRSArrTime: integer (nullable = true)
     |-- UniqueCarrier: string (nullable = true)
     |-- FlightNum: integer (nullable = true)
     |-- TailNum: string (nullable = true)
     |-- ActualElapsedTime: string (nullable = true)
     |-- CRSElapsedTime: string (nullable = true)
     |-- AirTime: string (nullable = true)
     |-- ArrDelay: string (nullable = true)
     |-- DepDelay: string (nullable = true)
     |-- Origin: string (nullable = true)
     |-- Dest: string (nullable = true)
     |-- Distance: integer (nullable = true)
     |-- TaxiIn: string (nullable = true)
     |-- TaxiOut: string (nullable = true)
     |-- Cancelled: integer (nullable = true)
     |-- CancellationCode: string (nullable = true)
     |-- Diverted: integer (nullable = true)
     |-- CarrierDelay: string (nullable = true)
     |-- WeatherDelay: string (nullable = true)
     |-- NASDelay: string (nullable = true)
     |-- SecurityDelay: string (nullable = true)
     |-- LateAircraftDelay: string (nullable = true)
    


Similar to RDD's, we can perform a `count` on a DataFrame to get the number of row. Notice that this already may take some time on small computers.


```python
f.count()
```




    7009728



lternatively you can manually specify the schema, but in this tutorial, we will stick with inferSchema. 

### (b) Casting values

Now we have a dataframe, we would like to compute the the average delays for departure and arrival per flight number. However, notice that inferschema sometimes guesses wrong: e.g. DepDelay and ArrDelay should be double instead of String. 

Also compute these statistics efficiently, we do not need the entire dataframe, but just a few columns. If we do not leave out the other columns, much more data is processed than necessary.


```python
from pyspark.sql.types import *

delays = f.select(  f.FlightNum.cast(IntegerType()), 
                                f.DepDelay.cast(DoubleType()),
                                f.ArrDelay.cast(DoubleType()))
delays.printSchema()
```

    root
     |-- FlightNum: integer (nullable = true)
     |-- DepDelay: double (nullable = true)
     |-- ArrDelay: double (nullable = true)
    


### (c) Providing a schema

Alternatively you can manually specify the schema. For this we will first extract the first line of the file, which contains the headers.


```python
from bz2 import BZ2File as bzopen
with bzopen(filename) as fin:
    header = fin.readline().decode('utf-8')
header
```




    'Year,Month,DayofMonth,DayOfWeek,DepTime,CRSDepTime,ArrTime,CRSArrTime,UniqueCarrier,FlightNum,TailNum,ActualElapsedTime,CRSElapsedTime,AirTime,ArrDelay,DepDelay,Origin,Dest,Distance,TaxiIn,TaxiOut,Cancelled,CancellationCode,Diverted,CarrierDelay,WeatherDelay,NASDelay,SecurityDelay,LateAircraftDelay\n'



Now we remove the \n and split on ',' to get the column names.


```python
columns = header.strip().split(',')
columns[:10]
```




    ['Year',
     'Month',
     'DayofMonth',
     'DayOfWeek',
     'DepTime',
     'CRSDepTime',
     'ArrTime',
     'CRSArrTime',
     'UniqueCarrier',
     'FlightNum']



In this very short example, we will assume that all columns are Strings except for the ones that we modify. To create a schema, we eventually need an ordered List of StructField's. However, to change a few fields, it is easier to setup a dictionary to make the changes and then convert it into an ordered list.


```python
from pyspark.sql.types import *

# maps a StringType structfield to each column name
fields = {column:StructField(column, StringType()) for column in columns}

# change the two delays to IntType
fields['ArrDelay'] = StructField('ArrDelay', IntegerType())
fields['DepDelay'] = StructField('DepDelay', IntegerType())

# change the dictinary to an ordered list
fields = [fields[column] for column in columns]
# and construct a schema from the fields
schema = StructType(fields)

# lets inspect the schema, ArrDelay and DepDelay should be IntegerType
schema

```




    StructType(List(StructField(Year,StringType,true),StructField(Month,StringType,true),StructField(DayofMonth,StringType,true),StructField(DayOfWeek,StringType,true),StructField(DepTime,StringType,true),StructField(CRSDepTime,StringType,true),StructField(ArrTime,StringType,true),StructField(CRSArrTime,StringType,true),StructField(UniqueCarrier,StringType,true),StructField(FlightNum,StringType,true),StructField(TailNum,StringType,true),StructField(ActualElapsedTime,StringType,true),StructField(CRSElapsedTime,StringType,true),StructField(AirTime,StringType,true),StructField(ArrDelay,IntegerType,true),StructField(DepDelay,IntegerType,true),StructField(Origin,StringType,true),StructField(Dest,StringType,true),StructField(Distance,StringType,true),StructField(TaxiIn,StringType,true),StructField(TaxiOut,StringType,true),StructField(Cancelled,StringType,true),StructField(CancellationCode,StringType,true),StructField(Diverted,StringType,true),StructField(CarrierDelay,StringType,true),StructField(WeatherDelay,StringType,true),StructField(NASDelay,StringType,true),StructField(SecurityDelay,StringType,true),StructField(LateAircraftDelay,StringType,true)))



Read the file as a DataFrame using the given schema. The csv reader will automatically decompress bz2. You should see that for large files, providing a schema is much faster than inferring a schema. It is also much cleaner than reading everything as a string and casting all types (although, for accessing a single column casting is much more convenient).


```python
delays = spark.read.csv(filename, header=True, schema=schema)
delays
```




    DataFrame[Year: string, Month: string, DayofMonth: string, DayOfWeek: string, DepTime: string, CRSDepTime: string, ArrTime: string, CRSArrTime: string, UniqueCarrier: string, FlightNum: string, TailNum: string, ActualElapsedTime: string, CRSElapsedTime: string, AirTime: string, ArrDelay: int, DepDelay: int, Origin: string, Dest: string, Distance: string, TaxiIn: string, TaxiOut: string, Cancelled: string, CancellationCode: string, Diverted: string, CarrierDelay: string, WeatherDelay: string, NASDelay: string, SecurityDelay: string, LateAircraftDelay: string]



Internally, every row in de DataFrame is modelled as a *Row* object that contains a dictionary of values.


```python
delays.take(5)
```




    [Row(Year='2008', Month='1', DayofMonth='3', DayOfWeek='4', DepTime='2003', CRSDepTime='1955', ArrTime='2211', CRSArrTime='2225', UniqueCarrier='WN', FlightNum='335', TailNum='N712SW', ActualElapsedTime='128', CRSElapsedTime='150', AirTime='116', ArrDelay=-14, DepDelay=8, Origin='IAD', Dest='TPA', Distance='810', TaxiIn='4', TaxiOut='8', Cancelled='0', CancellationCode=None, Diverted='0', CarrierDelay='NA', WeatherDelay='NA', NASDelay='NA', SecurityDelay='NA', LateAircraftDelay='NA'),
     Row(Year='2008', Month='1', DayofMonth='3', DayOfWeek='4', DepTime='754', CRSDepTime='735', ArrTime='1002', CRSArrTime='1000', UniqueCarrier='WN', FlightNum='3231', TailNum='N772SW', ActualElapsedTime='128', CRSElapsedTime='145', AirTime='113', ArrDelay=2, DepDelay=19, Origin='IAD', Dest='TPA', Distance='810', TaxiIn='5', TaxiOut='10', Cancelled='0', CancellationCode=None, Diverted='0', CarrierDelay='NA', WeatherDelay='NA', NASDelay='NA', SecurityDelay='NA', LateAircraftDelay='NA'),
     Row(Year='2008', Month='1', DayofMonth='3', DayOfWeek='4', DepTime='628', CRSDepTime='620', ArrTime='804', CRSArrTime='750', UniqueCarrier='WN', FlightNum='448', TailNum='N428WN', ActualElapsedTime='96', CRSElapsedTime='90', AirTime='76', ArrDelay=14, DepDelay=8, Origin='IND', Dest='BWI', Distance='515', TaxiIn='3', TaxiOut='17', Cancelled='0', CancellationCode=None, Diverted='0', CarrierDelay='NA', WeatherDelay='NA', NASDelay='NA', SecurityDelay='NA', LateAircraftDelay='NA'),
     Row(Year='2008', Month='1', DayofMonth='3', DayOfWeek='4', DepTime='926', CRSDepTime='930', ArrTime='1054', CRSArrTime='1100', UniqueCarrier='WN', FlightNum='1746', TailNum='N612SW', ActualElapsedTime='88', CRSElapsedTime='90', AirTime='78', ArrDelay=-6, DepDelay=-4, Origin='IND', Dest='BWI', Distance='515', TaxiIn='3', TaxiOut='7', Cancelled='0', CancellationCode=None, Diverted='0', CarrierDelay='NA', WeatherDelay='NA', NASDelay='NA', SecurityDelay='NA', LateAircraftDelay='NA'),
     Row(Year='2008', Month='1', DayofMonth='3', DayOfWeek='4', DepTime='1829', CRSDepTime='1755', ArrTime='1959', CRSArrTime='1925', UniqueCarrier='WN', FlightNum='3920', TailNum='N464WN', ActualElapsedTime='90', CRSElapsedTime='90', AirTime='77', ArrDelay=34, DepDelay=34, Origin='IND', Dest='BWI', Distance='515', TaxiIn='3', TaxiOut='10', Cancelled='0', CancellationCode=None, Diverted='0', CarrierDelay='2', WeatherDelay='0', NASDelay='0', SecurityDelay='0', LateAircraftDelay='32')]



And together with the schema, show presents the data as a readable table (provided a monospaced font is used).


```python
delays.show(5)
```

    +----+-----+----------+---------+-------+----------+-------+----------+-------------+---------+-------+-----------------+--------------+-------+--------+--------+------+----+--------+------+-------+---------+----------------+--------+------------+------------+--------+-------------+-----------------+
    |Year|Month|DayofMonth|DayOfWeek|DepTime|CRSDepTime|ArrTime|CRSArrTime|UniqueCarrier|FlightNum|TailNum|ActualElapsedTime|CRSElapsedTime|AirTime|ArrDelay|DepDelay|Origin|Dest|Distance|TaxiIn|TaxiOut|Cancelled|CancellationCode|Diverted|CarrierDelay|WeatherDelay|NASDelay|SecurityDelay|LateAircraftDelay|
    +----+-----+----------+---------+-------+----------+-------+----------+-------------+---------+-------+-----------------+--------------+-------+--------+--------+------+----+--------+------+-------+---------+----------------+--------+------------+------------+--------+-------------+-----------------+
    |2008|    1|         3|        4|   2003|      1955|   2211|      2225|           WN|      335| N712SW|              128|           150|    116|     -14|       8|   IAD| TPA|     810|     4|      8|        0|            null|       0|          NA|          NA|      NA|           NA|               NA|
    |2008|    1|         3|        4|    754|       735|   1002|      1000|           WN|     3231| N772SW|              128|           145|    113|       2|      19|   IAD| TPA|     810|     5|     10|        0|            null|       0|          NA|          NA|      NA|           NA|               NA|
    |2008|    1|         3|        4|    628|       620|    804|       750|           WN|      448| N428WN|               96|            90|     76|      14|       8|   IND| BWI|     515|     3|     17|        0|            null|       0|          NA|          NA|      NA|           NA|               NA|
    |2008|    1|         3|        4|    926|       930|   1054|      1100|           WN|     1746| N612SW|               88|            90|     78|      -6|      -4|   IND| BWI|     515|     3|      7|        0|            null|       0|          NA|          NA|      NA|           NA|               NA|
    |2008|    1|         3|        4|   1829|      1755|   1959|      1925|           WN|     3920| N464WN|               90|            90|     77|      34|      34|   IND| BWI|     515|     3|     10|        0|            null|       0|           2|           0|       0|            0|               32|
    +----+-----+----------+---------+-------+----------+-------+----------+-------------+---------+-------+-----------------+--------------+-------+--------+--------+------+----+--------+------+-------+---------+----------------+--------+------------+------------+--------+-------------+-----------------+
    only showing top 5 rows
    


We can reduce the columns in the dataframe by using `select` with a list of columns. A column can either be addressed by its name, or as a property of the dataframe. Reducing the columns in a DataFrame may improve efficiency, since less data is processed.


```python
delays1 = delays.select(delays.FlightNum, delays.ArrDelay, delays.DepDelay)
delays1.printSchema()
delays1.show(5)
```

    root
     |-- FlightNum: string (nullable = true)
     |-- ArrDelay: integer (nullable = true)
     |-- DepDelay: integer (nullable = true)
    
    +---------+--------+--------+
    |FlightNum|ArrDelay|DepDelay|
    +---------+--------+--------+
    |      335|     -14|       8|
    |     3231|       2|      19|
    |      448|      14|       8|
    |     1746|      -6|      -4|
    |     3920|      34|      34|
    +---------+--------+--------+
    only showing top 5 rows
    


Tests show that in general DataFrames are quite a bit faster than RDD's. However, the API for DataFrame is declarative, meaning that we describe how we want our data to look (like in SQL) rather than describing how to transform the data (like RDD's). This declarative API does not directly support lambda functions to process elements. However, in the background, RDD's are still used to process the data, so the code is still lazy executed, e.g. the next example will terminate after the first 5 matches are found. 


```python
delays1.filter(delays.FlightNum == 335).show(5)
```

    +---------+--------+--------+
    |FlightNum|ArrDelay|DepDelay|
    +---------+--------+--------+
    |      335|     -14|       8|
    |      335|     -17|       2|
    |      335|       2|       3|
    |      335|     -17|       7|
    |      335|       8|      24|
    +---------+--------+--------+
    only showing top 5 rows
    


The `select()` transformation can be used to apply expressions to columns, and `alias()` to name the resulting columns. 


```python
delays1.select(delays1.FlightNum, (delays1.ArrDelay - delays1.DepDelay).alias('diff')).show(5)
```

    +---------+----+
    |FlightNum|diff|
    +---------+----+
    |      335| -22|
    |     3231| -17|
    |      448|   6|
    |     1746|  -2|
    |     3920|   0|
    +---------+----+
    only showing top 5 rows
    


Spark DataFrames also have an SQL like syntax for expressions. Instead of `delays.DepDelay` we can also give the column name `'DepDelay'` which is resolved by looking up the column with that name in the DataFrame. Spark creates new column objects when a new DataFrame is created, and these columns do not always correspond to the original columns. In the example below the groupBy causes new columns to be created, and therefore `delays.DepDelay` would not work and `'DepDelays'` does.


```python
delays1.groupBy( delays1.FlightNum).avg( 'DepDelay', 'ArrDelay').show(5)
```

    +---------+------------------+------------------+
    |FlightNum|     avg(DepDelay)|     avg(ArrDelay)|
    +---------+------------------+------------------+
    |      829| 8.810008481764207| 7.094147582697201|
    |      675| 7.604868913857678| 6.379525593008739|
    |      467|11.521201413427562|10.378091872791519|
    |     2294| 7.268108108108108| 4.548108108108108|
    |     3414|5.0588235294117645| 4.901960784313726|
    +---------+------------------+------------------+
    only showing top 5 rows
    


To perform more complex aggregation functions you can call the generic `agg` tranformation on a DataFrame. Additionally, the `sql.functions` libraray supplies standard functions you can use, e.g. sum, max, count, mean.


```python
from pyspark.sql.functions import *

fg = delays1.groupBy(delays.FlightNum)
fg = fg.agg(mean(delays1.ArrDelay - delays1.DepDelay), mean("ArrDelay"))
fg.show(5)
```

    +---------+--------------------------+------------------+
    |FlightNum|avg((ArrDelay - DepDelay))|     avg(ArrDelay)|
    +---------+--------------------------+------------------+
    |      829|        -1.715860899067006| 7.094147582697201|
    |      675|       -1.2253433208489388| 6.379525593008739|
    |      467|       -1.1431095406360423|10.378091872791519|
    |     2294|                     -2.72| 4.548108108108108|
    |     3414|       -0.1568627450980392| 4.901960784313726|
    +---------+--------------------------+------------------+
    only showing top 5 rows
    


Although functions cannot be used by mapping them to elements in the same way as with RDD's, we can create a User Defined Function (UDF) based on a function and use that. For this, `udf` must be imported from `spark.sql.functions` which we already did above.

One catch with udf's is that they return a String by default, converting the output value in the process. We can prevent the `udf` from returning a String by specifying a return type.

NB Spark also has User Defined Aggregate Functions (UDAF), but currently these cannot currently be used from Python (you have to write it in Scala or Java). 


```python
diff = udf(lambda x, y: x - y, DoubleType())

# filter out empty cells
delaysnn = delays.filter(delays.ArrDelay.isNotNull()).filter(delays.DepDelay.isNotNull())
delaysnn.select(delays.FlightNum, diff(delays.ArrDelay, delays.DepDelay)).show(5)
```

    +---------+----------------------------+
    |FlightNum|<lambda>(ArrDelay, DepDelay)|
    +---------+----------------------------+
    |      335|                        null|
    |     3231|                        null|
    |      448|                        null|
    |     1746|                        null|
    |     3920|                        null|
    +---------+----------------------------+
    only showing top 5 rows
    


## DataFrame to RDD ##

For some operations, RDD provides a more convenient API than DataFrames. For example, to take the 5 longest departure delays for each flight is very hard to do with DataFrames, since there is no standard function in the DataFrame API and no way to group and flatten data. However, we can also use RDD functions by simply converting a DataFrame to an RDD.

Working with RDD's becomes easier when the data has a (key, value) structure. We can prepare that by selecting just the flight number and departure delay. We need to filter out the empty cells. Converting the DataFrame to RDD is as simple as accessing the rdd property of the DataFrame. When we inspect the RDD format, we would see that every element is a DataFrame `Row` object, however, these work just the same as any other tuple. We group the by the key, and can then sort the departure delays per group and take the last 5. We can also flatten the structure so that we get single (FligtNum, DepDelay) elements in one go by using `flatMapValues` instead of `mapValues`.


```python
depdelays = delays.select(delays.FlightNum, delays.DepDelay)
depdelays = depdelays.filter(depdelays.DepDelay.isNotNull())
delaysrdd = depdelays.rdd
delaysrdd = delaysrdd.groupByKey().\
                      mapValues(lambda x : sorted(x)[-5:])
delaysrdd.take(5)
```




    [('670', [230, 241, 328, 353, 611]),
     ('815', [260, 266, 293, 309, 386]),
     ('2699', [160, 180, 195, 220, 243]),
     ('3746', [228, 228, 229, 336, 813]),
     ('594', [211, 217, 254, 322, 323])]




```python

```


```python

```
