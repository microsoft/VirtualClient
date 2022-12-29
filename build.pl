#!/usr/bin/perl -l
#
# Build the source code (on Windows or Linux) systems.

# printf "Arg Count = %d", $#ARGV;
# printf "Arg 0 = %s", $ARGV[0];
# printf "Arg 1 = %s", $ARGV[1];
use strict;
use warnings;
use FindBin;

my $EXIT_CODE = 0;

if ($#ARGV > -1) {
    if ($ARGV[0] == "/?" || $ARGV[0] == "-?" || $ARGV[0] == "-help" || $ARGV[0] == "--help") {
        goto USAGE;
    }
}

$ENV{VCSolutionDir} = join("/", FindBin::again(), "src", "VirtualClient");

print "";
print "-------------------------------------------------------";
print "Build Source Code Version 1.2.3.4";
print "-------------------------------------------------------";
print "";
print "[Build Solution]";
my $output = qx(dotnet build "$ENV{VCSolutionDir}/VirtualClient.sln" 2>&1);
print "";
print $output;

if ($? > 0) {
    goto END;
}

print "[Build Main]";
$output = qx(dotnet publish "$ENV{VCSolutionDir}/VirtualClient.Main/VirtualClient.Main.csproj" -c Debug 2>&1);
print "";
print $output;

if ($? > 0) {
    goto END;
}

print "[Build Main -> linux-x64]";
$output = qx(dotnet publish "$ENV{VCSolutionDir}/VirtualClient.Main/VirtualClient.Main.csproj" -c Debug -r linux-x64 --self-contained -p:InvariantGlobalization=true -p:PublishTrimmed=true -p:TrimUnusedDependencies=true 2>&1);
print "";
print $output;

if ($? > 0) {
    goto END;
}

printf "Exit Code = %d", $?;
print $output;
goto END;

USAGE:
print "";
print "Usage:";
print "---------------------";
print $0;
print "$0 {buildVersion}";
goto EXIT;

END:
printf "Build Stage Exit Code: %d", $?;
print "";

EXIT:
exit $?;