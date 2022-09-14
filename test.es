GET 0xdac17f958d2ee523a2206206994597c13d831ec7_1/_search

GET 0xdac17f958d2ee523a2206206994597c13d831ec7_1/_doc/5324650_94

POST _reindex
{
  "source": {
    "query": {
      "match": {
        "__event": "Transfer"
      }
    },
    "index": "0xdac17f958d2ee523a2206206994597c13d831ec7_2"
  },
  "dest": {
    "index": "0xdac17f958d2ee523a2206206994597c13d831ec7_2_v1"
  }
}

GET 0xdac17f958d2ee523a2206206994597c13d831ec7_1_v4/_search

GET 0xdac17f958d2ee523a2206206994597c13d831ec7_1_v3/_search?size=0
{
  "aggs": {
    "total" : {
      "sum": {"field": "value"}
    }
  }
}

//
GET 0xdac17f958d2ee523a2206206994597c13d831ec7_2/_count

GET 0xdac17f958d2ee523a2206206994597c13d831ec7_2/_search

PUT usdt
{
  "mappings": {
    "properties": {
      "from":     { "type": "keyword" },
      "to":       { "type": "keyword" },
      "value":    { "type": "unsigned_long" }
    }
  }
}

POST _reindex
{
  "source": {
    "query": {
      "match": {
        "__event": "Transfer"
      }
    },
    "index": "0xdac17f958d2ee523a2206206994597c13d831ec7_2"
  },
  "dest": {
    "index": "usdt"
  }
}

GET usdt/_count

GET usdt/_search
{
  "sort" : [
    {"__block" : "asc"},
    {"__index" : "asc"}
  ]
}

GET usdt/_search?size=0
{
  "aggs": {
    "total" : {
      "sum": {"field": "value"}
    }
  }
}

GET usdt/_search?size=0
{
  "aggs": {
    "to": {
      "terms": { 
        "size": 20,
        "field": "to",
        "order": {
          "total": "desc"
        }
      },
      "aggs": {
        "total": {
          "sum" : {"field": "value"}
        }
      }
    }
  }
}

PUT _transform/usdt-value-sum
{
  "source": {
    "index": [
      "usdt*"
    ]
  },
  "pivot": {
    "group_by": {
      "to": {
        "terms": {
          "field": "to"
        }
      }
    },
    "aggregations": {
      "value.sum": {
        "sum": {
          "field": "value"
        }
      }
    }
  },
  "frequency": "1m",
  "dest": {
    "index": "value-sum_usdt"
  },
  "settings": {
    "max_page_search_size": 500
  }
}



GET value-sum-usdt/_search