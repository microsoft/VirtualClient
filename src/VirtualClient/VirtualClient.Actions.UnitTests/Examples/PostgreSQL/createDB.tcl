#!/bin/tclsh
puts "SETTING CONFIGURATION"

#global complete
#proc wait_to_complete {} {
#global complete
#set complete [vucomplete]
#if {!$complete} {after 5000 wait_to_complete} else { exit }
#}

dbset db pg
dbset bm TPC-C
diset connection pg_host localhost
diset connection pg_port 5432
diset tpcc pg_count_ware <WAREHOUSECOUNT>
diset tpcc pg_num_vu <VIRTUALUSERS>
diset tpcc pg_superuser postgres
diset tpcc pg_superuserpass postgres
diset tpcc pg_defaultdbase postgres
diset tpcc pg_user <USERNAME>
diset tpcc pg_pass <PASSWORD>
diset tpcc pg_dbase tpcc
print dict
buildschema
#vustatus
#if { [vucomplete] } {puts "its complete"} else {puts "it's not complete"}
#if { [vucomplete] } {after 5000 wait_to_complete} else { exit }
waittocomplete
#wait_to_complete
#exit
