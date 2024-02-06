nuget restore "ZZZO\ZZZO.sln"
msbuild "ZZZO\ZZZO.sln" /t:Build /p:Configuration=Release
7z a -tzip -mx9 "zzzo-latest.zip" ".\ZZZO\ZZZO\bin\Release\*"

ls