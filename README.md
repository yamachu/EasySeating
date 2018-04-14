# EasySeating
AzureFunctionsを使ったSlack Slash Commandの席替えコマンド

## How to develop

```shell
$ cp settings/local.settings.json.tmpl local.settings.json
$ dotnet build
```

## How to debug

```shell
$ cd bin/Debug/netstandard2.0 && func start host
$ # open another shell
$ curl -X POST 'here is local functions url' -d 'token=YOUR_TOKEN&response_url=ITS_DUMMY_URL&channel_id=YOUR_CHANNEL_ID&text=3 5 7 5'
```

## How to deploy

```shell
$ func azure functionapp publish YOUR_FUNCTION_NAME
```

enjoy
