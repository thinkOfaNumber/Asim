set MSBUILDEXE=C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe

echo Building...
%MSBUILDEXE% "/property:Configuration=Release" ..\SolarLoadModel\SolarLoadModel.csproj

echo Running...
..\SolarLoadModel\bin\Release\SolarLoadModel.exe --StartTime "1/01/2012 00:00:00" --iterations 604800 ^
 --directory ..\Data\ ^
 --input Example_GenStats.csv ^
 --input Example_StationStats.csv ^
 --input Example_GenConfigurations.csv ^
 --input Example_Solar.csv ^
 --input "daly river 1s.csv" ^
 --input Example_FuelEfficiency.csv ^
 --output output.csv 1 Gen1* ^
 --output weekly.csv 604800 Gen1*Cnt
