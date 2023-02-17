#!/bin/tclsh
proc runtimer { seconds } {
set x 0
set timerstop 0
#set z 16
while {!$timerstop} {
incr x
after 1000
if { ![ expr {$x % 60} ] } {
set y [ expr $x / 60 ]
puts "Timer: $y minutes elapsed"
}
update
if { [ vucomplete ] || $x eq $seconds } { set timerstop 1 }
}
return
}

puts "SETTING CONFIGURATION"
dbset db pg
dbset bm TPC-C
diset connection pg_host <HOSTNAME>
diset connection pg_port 5432
diset tpcc pg_superuser postgres
diset tpcc pg_superuserpass postgres
diset tpcc pg_defaultdbase postgres
#diset tpcc pg_user nmalkapuramuser
#diset tpcc pg_pass 1234
diset tpcc pg_user <USERNAME>
diset tpcc pg_pass <PASSWORD>
diset tpcc pg_dbase tpcc
diset tpcc pg_driver timed
diset tpcc pg_duration 2
diset tpcc pg_duration 5
diset tpcc pg_vacuum true
print dict
vuset logtotemp 1
loadscript
puts "SEQUENCE STARTED"
set z <VIRTUALUSERS>
#set z 16
#foreach z { 1 16 32} {
puts "$z VU TEST"
vuset vu $z
vucreate
vurun
runtimer 10000
vudestroy
after 5000
#}
puts "TEST SEQUENCE COMPLETE"
exit
