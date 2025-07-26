# BasicHttpServer Demo

This demo scene hosts an HTTP server on port 8080.

## Endpoints

### Hello

Get data from server.

Example:

```bash
curl -v http://127.0.0.1:8080/hello
```

### Toast

Print a toast message in Unity with data sent over HTTP.

Example:

```bash
curl -v http://127.0.0.1:8080/toast -X POST --data 'message'
```
