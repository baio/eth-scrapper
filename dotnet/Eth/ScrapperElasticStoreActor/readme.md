$Env:PORT=5003

dapr run --app-port $Env:Port --app-id scrapper-elastic-store --components-path ../../../components --dapr-http-port 3503 -- dotnet watch run

