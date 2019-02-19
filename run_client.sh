if [ $# -ne 1 ]
then
  echo "Specify connectionString"
  exit 1
fi

connectionString=$1
clientPostfix=`date --iso-8601='seconds'`
dotnet run -- client "HubClient${clientPostfix}" -c "$connectionString" -h Chat
