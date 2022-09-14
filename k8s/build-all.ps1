docker build --build-arg PORT=3000 -t baio/eth-scrapper-dispatcher-actor -f ./dotnet/Eth/ScrapperDispatcherActor/Dockerfile ./dotnet/Eth
docker push baio/eth-scrapper-dispatcher-actor:latest
kubectl apply -f ./baio/scrapper-dispatcher-actor.yaml
