dotnet publish "ZZZO\ZZZO\ZZZO.csproj" -p:PublishProfile=FolderProfile
7z a -tzip -mx9 "zzzo-latest.zip" ".\ZZZO\ZZZO\bin\Release\net6.0-windows\publish\win-x86\*"

ls