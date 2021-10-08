#### 0.1.1 October 08 2021 ####
Modified the default HTTP span name to be

```
HTTP {METHOD} {PATH}
```

So spans should now be reported as "HTTP POST /myapi/v1/{entityId}"

#### 0.1.0 October 08 2021 ####
Fist implementation of Nancy.OpenTracing, designed to work with Nancy 1.4.1 and OpenTracing 0.12.1.