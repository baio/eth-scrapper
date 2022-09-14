# Ethereum Scrapper

Solution to scarp ethereum event logs and store to elasticsearch.

## Quick start

```
docker-compose up
```

Then open browser `http://localhost:6002`

![home](assets/home.jpg "home")

+ Setup you ethereum provider url, most often its from infura like this `https://mainnet.infura.io/v3/xxx`

+ Add contract address to scrap
+ Click `start` button

Start process will run, logs will be stored to  elasticsearh (started in docker container, data volume mounted to `.docker-data/elasticsearch`)

Use refresh button to update progress.
Scrapper could be paused and resumed, progress state will be stored. If some error occurs, scrapper will be set to `Failure` state, after it could be resumed

