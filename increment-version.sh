calculate_new_version(){
    patch=$(echo $1 | cut -d '.' -f3 | cut -d '"' -f1)
    minor=$(echo $1 | cut -d '.' -f2)
    major=$(echo $1 | cut -d '.' -f1 | cut -d '"' -f2)

    ((patch++))

    echo "$major.$minor.$patch"
}

current_version=$(yq '.variables.version' .pipelines/azure.pipelines.yml)
echo "current version: $current_version"

git config --global advice.detachedHead false

git stash
git checkout HEAD~1
previous_version=$(yq '.variables.version' .pipelines/azure.pipelines.yml)
echo "previous version: $previous_version"

git checkout main

if [ "$previous_version" == "$current_version" ]; then
    echo "versions match, have to update now"
    new_version=$(calculate_new_version $current_version)
    echo "new version: $new_version"

    key="version"
    new_value=$new_version
    yaml_file=".pipelines/azure.pipelines.yml"

    sed -r "s/^(\\s*${key}\\s*:\\s*).*/\\1${new_value}/" -i "$yaml_file"

    git config user.email "sai.bulusu@gmail.com"
    git config user.name "saibulusu"
    git add -f .pipelines/azure.pipelines.yml
    git commit -m "Updating to $new_version"
    git push
else
    echo "version already updated, no need to update further"
fi
