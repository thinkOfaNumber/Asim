#!/bin/sh

TOTAL_TESTS=0
PASSED_TESTS=0
NOERROR=0
ERROR=1


function FindCsv() {
	# build a pattern of files to exclude
	echo function FindCsv >>log
	/bin/ls | grep -v -E '.csv$' > grep-exclude &&
	/bin/ls ../../tests/$1 | grep -v -E '.csv$' >> grep-exclude &&
	echo grep-exclude >> grep-exclude &&
	return $NOERROR;
	return $ERROR;
}

function CompareOutput() {
	echo function CompareOutput >>log
	diff -q -r --exclude-from=grep-exclude . ../../tests/$1 >>log
}

function ExcelName() {
	echo function ExcelName >>log
	read str >>log 2>>log < "run" ;
	echo -n $str;
}

function RunSimulator() {
	echo function RunSimulator >>log
	excelName=$(ExcelName);
	echo excel name is \'$excelName\' >>log;
	if [ -z "$excelName" ]; then
		return $ERROR;
	fi
	bin/ExcelReader.exe --nodate --input $excelName >>log;
	ret=$?;
	if (( $ret == 0 )); then
		return $NOERROR;
	fi
	return $ERROR;
}

function RunTest() {
	echo -n "running test $1...";
	cp -r tests/$1 results &&
	rm -rf results/$1/bin &&
	cp -r bin results/$1 &&
	cd results/$1 &&
	echo function RunTest > log  &&
	RunSimulator &&
	FindCsv $1 &&
	CompareOutput $1;
	if (( $? == $NOERROR )); then
		echo -e "\tOK";
		(( PASSED_TESTS ++ ));
	else
		echo -e "\tFAIL";
	fi
}

function RunAllTests () {
	rm -rf results;
	mkdir results;
	testdir=`pwd`;
	for test; do
		cd $testdir;
		(( TOTAL_TESTS ++ ));
		RunTest $test;
	done
}

RunAllTests `ls tests`
echo $PASSED_TESTS / $TOTAL_TESTS tests passed.
