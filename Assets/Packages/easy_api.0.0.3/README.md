
# EasyAPI for Unity

EasyAPI is a lightweight Unity plugin for handling REST API requests (GET, POST, PUT, DELETE) with ease. Designed for Unity developers who need to connect to RESTful APIs, EasyAPI provides async-friendly calls, token-based authentication, and simple response handling.

## Features
- Supports GET, POST, PUT, and DELETE requests.
- Async support with `UniTask` for smoother performance.
- Built-in authentication support (Bearer and Basic tokens).
- Callback support for success and failure responses.

## Installation
1. Install the package from NuGet.
2. Import the namespace:
   ```csharp
   using EasyAPIPlugin;
   ```
## Sample API Call
```csharp
 List<Post> posts = await EasyAPI.Get<List<Post>>("https://jsonplaceholder.typicode.com/posts");
 Debug.Log($"post:{JsonConvert.SerializeObject(posts)}");
```

