if [ $# -ne 1 ]
then
  echo "Specify connectionString"
  exit 1
fi

connectionString=$1
dotnet run -- server -c "$connectionString" -h Chat
