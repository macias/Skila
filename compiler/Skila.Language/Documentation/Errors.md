## Errors

Currently errors are returned as `Option<T>` meaning the only info
there returned is whether the error occurred or not. The rationale
for such approach is need to set performance at the same level as 
C/C# with magic values, thus failure and success scenario cannot
drag along stack trace or anything like that.

If this won't work maybe we should return richer value, with
`Symbol` (similar to `String` but with singular values, and const at compile time)
instead of `Bool`.