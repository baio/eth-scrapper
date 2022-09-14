# Scrapper actor

Read events given address, abi, and block from (opt) to (opt)

```
$Env:PORT=5002

dapr run --app-port $Env:Port --app-id scrapper-actor --components-path ../../components --dapr-http-port 3502 -- npm run start
```

with build 

```
cd build 

$Env:PORT=5002

dapr run --app-port $Env:Port --app-id scrapper-actor --components-path ../../../components --dapr-http-port 3502 -- node index.js
```
