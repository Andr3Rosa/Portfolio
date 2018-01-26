
## RDF ##

The Resource Description Framework is an infrastructure that enables the encoding, exchange and reuse of structured metadata. A common variant for knowledge bases is a triple store in which atomic facts are stored. Every record is structured as `<subject> <predicate> <value>`, where the  predicate describes the relation between the `subject` and the `value`. The `subject` is usually an identifier for an entity (e.g. movie, person, event) and the value can be either a literal value (e.g. String, int) or also an identfier for an entity. The predicates and identifier are described in ontologies, providing a standard for semantics and to facilitate combining knowledge over different bases.

http://www.wikidata.org is one of the many projects that collects and stores knowledge in the world in a publicly available knowledge base. You can search the site for entities, and browse the facts stored (e.g. https://www.wikidata.org/wiki/Q83495 for movie The Matrix). In the entity browser identifiers are displayed with their text label, and linked to the identifiers' page.

## Sparql ##

Wikidata has a public Sparql endpoint, which we can use to collect data using Sparql queries. You can visit https://query.wikidata.org/ to enter sparql queries online, but below we will demonstrate them from Python.

### Prefixes ###

Identifiers in a triple store consist of the ontology domain and an id. Because the identifier names become very long, prefixes are commonly used to shorten the identifiers. For instance, the movie 'The Matrix' has identifier www.wikidata.org/entity/Q83495. By supplying the prefixes below, we can refer to the same entity as `wd:Q83495`.


```python
prefixes = """
PREFIX wd: <http://www.wikidata.org/entity/>
PREFIX wdt: <http://www.wikidata.org/prop/direct/>
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
"""
```

## Sparql query ##

Sparql queries scan the entire triple store and match rules in the `where` clause. Each rule is formatted as (!! ending with a .):

`subject predicate object .`

The rules can contain variable, whose names are preceded by a questionmark (e.g. ?movieid, ?title). The non-variables in a rule are requirements that must be met. For instance, wdt:P31 is the predicate 'instance of' and wd:Q11424 is the identifier for the Film-class. Thus the first rule matches all identifiers ?movieid that occur in a tripe ?movieid 'instance of' Film, or all movie identifiers. (You can easily find identifiers for predicates and values by browsing to a entity page (e.g. page for The Matrix above) and looking at the popups of the predicates).

The second rule matches triples that have a 'has label' predicate and shares the ?movieid variable with the first rule. This means that for every movie identifier it tries to find a label (name) for that identifier. Having multiple rules is similar to creating an inner join that combines the rules.

Wikidata contains labels in many languages. The filter requires matched rules to have a ?title value in english. For every combination of two triples that satisfy these rules, an output record is generated with the movie identifier and title. 


```python
query = """
SELECT ?movieid ?title
WHERE { 
  ?movieid wdt:P31 wd:Q11424 .
  ?movieid rdfs:label ?title .
        
  filter ( lang(?title) = "en" )
}  
"""
```

### Execute Sparql queries ###

We can submit a sparql query to the wikidata endpoint and it returns the results. We will use SPARWLWrapper, which you will need to install by running the command below from the command line in a terminal:

pip install sparqlwrapper


```python
from SPARQLWrapper import SPARQLWrapper, JSON
endpoint = SPARQLWrapper(
    'https://query.wikidata.org/bigdata/namespace/wdq/sparql')
endpoint.setReturnFormat(JSON)
endpoint.setQuery(prefixes + query)
results = endpoint.query().convert()['results']['bindings']
```


```python
results[:1]
```




    [{'movieid': {'type': 'uri', 'value': 'http://www.wikidata.org/entity/Q372'},
      'title': {'type': 'literal',
       'value': 'We Live in Public',
       'xml:lang': 'en'}}]



The results in JSON format have been converted to Python dictionaries. We can view the first line. Within every column the 'value' contains the actual value that we want. Python also has a `map` function we can use. In the code below, the `list` function forces the evaluation of the lazy code.


```python
l = map(lambda x: (x['movieid']['value'], x['title']['value']), results)
list(l)[:10]
```




    [('http://www.wikidata.org/entity/Q372', 'We Live in Public'),
     ('http://www.wikidata.org/entity/Q593', 'A Gang Story'),
     ('http://www.wikidata.org/entity/Q595', 'The Intouchables'),
     ('http://www.wikidata.org/entity/Q1365', 'Swept Away'),
     ('http://www.wikidata.org/entity/Q32011', 'On the Town'),
     ('http://www.wikidata.org/entity/Q32217', 'Soldier Boyz'),
     ('http://www.wikidata.org/entity/Q32228', 'The Nutty Professor'),
     ('http://www.wikidata.org/entity/Q32241', 'Incident at Loch Ness'),
     ('http://www.wikidata.org/entity/Q32303', 'The Hunt'),
     ('http://www.wikidata.org/entity/Q2201', 'Kick-Ass')]



We can remove the url-path (assuming we do not need that) by splitting the wikidata identifier on '/' and taking only the last element. To use our results in Spark, we can parallelize the results.


```python
l = map(lambda x: (x['movieid']['value'], x['title']['value']), results)
r = map(lambda x: (x[0].split('/')[-1], x[1]), l)
movietitles = sc.parallelize(r)
movietitles.take(5)
```




    [('Q372', 'We Live in Public'),
     ('Q593', 'A Gang Story'),
     ('Q595', 'The Intouchables'),
     ('Q1365', 'Swept Away'),
     ('Q32011', 'On the Town')]



From wikidata we can also retrieve the imdbid, allowing to connect the data to the ratings in movielens. We can add a rule with predicate P345 meaning 'has imdb id' to join the imdb ids for the movies.


```python
query = """
SELECT ?movieid ?title ?imdbid
WHERE { 
  ?movieid wdt:P31 wd:Q11424 .
  ?movieid rdfs:label ?title .
  ?movieid wdt:P345 ?imdbid .
        
  filter ( lang(?title) = "en" )
}  
"""

endpoint.setQuery(prefixes + query)
results = endpoint.query().convert()['results']['bindings']

l = map(lambda x: (x['movieid']['value'], 
                   x['title']['value'],
                   x['imdbid']['value']), results)
r = map(lambda x: (x[0].split('/')[-1], x[1], x[2]), l)
movies = sc.parallelize(r)
movies.take(5)
```

Note that Wikidata may not allow for excessive queries, so if you want to collect a lot of information you may have to break up queries horizontally (i.e. only a few columns per query).


```python

```
