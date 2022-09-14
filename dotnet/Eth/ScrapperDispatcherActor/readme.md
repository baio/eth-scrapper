$Env:PORT=5001

dapr run --app-port $Env:Port --app-id scrapper-dispatcher-actor --components-path ../../../components --dapr-http-port 3501 -- dotnet watch run