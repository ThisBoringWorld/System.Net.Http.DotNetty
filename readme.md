# System.Net.Http.DotNetty

基于 `DotNetty.Codecs.Http` 实现的 `HttpMessageHandler` ; 目标框架为 `netstandard2.0`; 
基本实现了代理请求、代理Basic认证、请求超时等基本功能并测试；内部实现了一个简单的连接池及其回收机制，并通过了简单的验证；直接替换 `HttpClientHandler` 就能使用；整体上测试了基本的使用情况，比较正常。对比原生实现好像还没发现大优势，这个要自行体验；

## 1. 如何使用

#### 暂无Nuget，下载源码进行编译并引用

#### 示例方式都应该以单例模式使用，请求是线程安全的

### 1.1 以HttpMessageHandler的方式使用

#### 1.1.1 简单的使用方式：

```C#
var client = new HttpClient(new HttpDotNettyClientHandler());
```

#### 1.1.2 如果需要自定义某些配置的话：

```C#
var option = new DotNettyClientOptions()
{
    //设置可选项
};
var client = new HttpClient(new HttpDotNettyClientHandler(option));
```

然后使用 `HttpClient` 像以前一样进行请求就行了；

### 1.2 以DotNetty的方式使用

直接使用 `DotNettyHttpRequestExecutor` 类的对象进行请求；

#### 1.2.1 引用命名空间

注意有类型会和 `System.Net`，`System.Net.Http` 下的类型冲突

```C#
using System.Net.Http.DotNetty;
using DotNetty.Codecs.Http;
using DotNetty.Common.Utilities;
```

#### 1.2.2 创建执行器，请求，并获取结果

```C#
var option = new DotNettyClientOptions()
{
    //设置可选项
};
var executor = new DotNettyHttpRequestExecutor(option);

var uri = new Uri("http://www.baidu.com");

var request = new DefaultFullHttpRequest(HttpVersion.Http11, HttpMethod.Get, uri.PathAndQuery);

IFullHttpResponse response = null;
try
{
    response = await executor.ExecuteAsync(request, uri, proxyUri: null, CancellationToken.None).ConfigureAwait(false);

    //信息获取
    var html = response.Content.ReadString(response.Content.WriterIndex, Encoding.UTF8);
}
finally
{
    response.SafeRelease();
}
```

## 2. Link

 - [DotNetty](https://github.com/Azure/DotNetty)