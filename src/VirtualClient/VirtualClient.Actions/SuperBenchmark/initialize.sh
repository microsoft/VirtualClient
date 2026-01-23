# Ansible will use sudo which needs explicit password input. This command removes that step.
echo '$1 ALL=(ALL) NOPASSWD:ALL' | (sudo EDITOR='tee -a' visudo) 
# Remove any existing system-installed Ansible to avoid version conflicts
# The old Ansible 2.10 doesn't support modern collections required by SuperBench
sudo apt remove -y ansible || true
sudo pip3 uninstall -y ansible ansible-base ansible-core || true
# Install ansible-core compatible with Python 3.8 (Ubuntu 20.04)
# ansible-core 2.12-2.13 is the highest version compatible with Python 3.8
python3 -m pip install --user "ansible-core>=2.12,<2.14"
# Ensure the pip user-installed ansible is in PATH and takes precedence
export PATH=/home/$1/.local/bin:$PATH
# Configure Docker to use the data disk at /mnt to avoid filling up root filesystem
sudo mkdir -p /mnt/docker
sudo systemctl stop docker || true
# Backup existing docker data if it exists
if [ -d "/var/lib/docker" ]; then
    sudo rsync -aP /var/lib/docker/ /mnt/docker/ || true
fi
# Configure Docker daemon to use new data directory
echo '{"data-root": "/mnt/docker"}' | sudo tee /etc/docker/daemon.json
sudo systemctl start docker
# Command to install sb dependencies.
python3 -m pip install .
# Command to build sb - this will install Ansible collections
make postinstall 
# This command initiates /dev/nvidiactl and /dev/nvidia-uvm directories, which sb checks before running.
sudo docker run --rm --gpus all nvidia/cuda:11.0.3-base nvidia-smi 