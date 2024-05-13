calculate_new_version(){
    patch=$(echo $1 | cut -d '.' -f3 | cut -d '"' -f1)
    minor=$(echo $1 | cut -d '.' -f2)
    major=$(echo $1 | cut -d '.' -f1 | cut -d '"' -f2)

    ((patch++))

    echo "$major.$minor.$patch"
}

current_version=$(<VERSION)
echo "current version: $current_version"

git config --global advice.detachedHead false

git stash
git checkout HEAD~1
previous_version=$(<VERSION)
echo "previous version: $previous_version"

git checkout main

if [ "$previous_version" == "$current_version" ]; then
    echo "versions match, have to update now"
    new_version=$(calculate_new_version $current_version)
    echo "new version: $new_version"

    echo "$new_version" > VERSION

    git config user.email "virtualclient@microsoft.com"
    git config user.name "virtualclient-ms"
    git add -f VERSION
    git commit -m "Updating to $new_version"
    git push
else
    echo "version already updated, no need to update further"
fi