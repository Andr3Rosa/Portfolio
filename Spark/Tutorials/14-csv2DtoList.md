
## CSV 2D table to list

The file income.csv (from gapminder.org) contains a 2D matrix with the average income (estimated as the GPD/capita) per country per year. Since 2D matrices are a bit more difficult to process, we will show how to preprocess one, and let you convert another in the assignments.

Load the file as lines of text, and view the first line which contains the column labels.


```python
filename = 'income.csv'
# read the file as lines of text
income2d = sc.textFile(filename)
# grab the header of the file
income2dheader = income2d.first()
income2dheader
```




    'GDP per capita,1800,1801,1802,1803,1804,1805,1806,1807,1808,1809,1810,1811,1812,1813,1814,1815,1816,1817,1818,1819,1820,1821,1822,1823,1824,1825,1826,1827,1828,1829,1830,1831,1832,1833,1834,1835,1836,1837,1838,1839,1840,1841,1842,1843,1844,1845,1846,1847,1848,1849,1850,1851,1852,1853,1854,1855,1856,1857,1858,1859,1860,1861,1862,1863,1864,1865,1866,1867,1868,1869,1870,1871,1872,1873,1874,1875,1876,1877,1878,1879,1880,1881,1882,1883,1884,1885,1886,1887,1888,1889,1890,1891,1892,1893,1894,1895,1896,1897,1898,1899,1900,1901,1902,1903,1904,1905,1906,1907,1908,1909,1910,1911,1912,1913,1914,1915,1916,1917,1918,1919,1920,1921,1922,1923,1924,1925,1926,1927,1928,1929,1930,1931,1932,1933,1934,1935,1936,1937,1938,1939,1940,1941,1942,1943,1944,1945,1946,1947,1948,1949,1950,1951,1952,1953,1954,1955,1956,1957,1958,1959,1960,1961,1962,1963,1964,1965,1966,1967,1968,1969,1970,1971,1972,1973,1974,1975,1976,1977,1978,1979,1980,1981,1982,1983,1984,1985,1986,1987,1988,1989,1990,1991,1992,1993,1994,1995,1996,1997,1998,1999,2000,2001,2002,2003,2004,2005,2006,2007,2008,2009,2010,2011,2012,2013,2014,2015'



In this file structure, the columns represent years, the rows are countries, and in the matrix cells the values are the incomes. The easiest way to process the file is to convert the 2D table format into a 1 dimensional list where every element is (country, year, income). 

In the table, the key (country) is in the first column and the values are in the following colums. There We can split each lines by the ',' separator and then create key,value pairs, where the value is a list over the years. For example, `Albania, 20, 20, 40` would become `(Albania, [20, 20, 40])`.


```python
def splitkey(x):
    s = x.split(',')
    return (s[0], s[1:])

income2d = income2d.map(splitkey)
```

We can now extract the year labels from the first line (showing just the first 5).


```python
header = income2d.first()
income2d = income2d.filter(lambda x: x != header)
years = header[1]
years[:5]
```




    ['1800', '1801', '1802', '1803', '1804']



Next we want to attach the years in the header (first line) to the values of every other line. We can do that using Python's `zip` function. e.g. `(Albania, [20,30])` would become `[(Albania, 1800, 20), (Albania, 1801, 30)]`. By flattening the RDD, we obtain seperate elements for every (country, year, income) tuple.

To use zip, we need three lists of equal length, the code `[x[0]] * len(years)` takes the country name `x[0]` places it in a list `[x[0]]` and the `*` duplicates the element to match the length of years. `zip(a, b, c)` results in a new list with `(a[0], b[0], c[0]), (a[1], b[1], c[1])`, etc.


```python
income = income2d.flatMap(lambda x: list(zip([x[0]] * len(years), years, x[1])))
```

One final thing to make processing easier, is to convert the last column to a float.


```python
income = income.map(lambda x: (x[0], x[1], None if x[2] == '' else float(x[2])))
```

And show the first 5 occurrences with actual data.


```python
income.filter(lambda x: x[2] is not None).take(5)
```




    [('Afghanistan', '1800', 603.0),
     ('Afghanistan', '1801', 603.0),
     ('Afghanistan', '1802', 603.0),
     ('Afghanistan', '1803', 603.0),
     ('Afghanistan', '1804', 603.0)]




```python

```
