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
 --input "daly river Pv 1s.csv" ^
 --input transpose-test-input.csv ^
 --input Example_FuelEfficiency.csv ^
 --output output.csv 1 Gen*LoadFact,Gen[0-9]P,LoadP,StatP,StatSpinP,GenP,PvAvailP,PvP,PvSpillP,GenMinRunT,GenSetCfg,GenOnlineCfg ^
 --output weekly.csv 604800 Gen*Cnt,StatBlack,GenOverload,StatBlackCnt ^
 --output test.csv 3600 *Pact,*Fact
