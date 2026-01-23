# Ansible will use sudo which needs explicit password input. This command removes that step.
echo '$1 ALL=(ALL) NOPASSWD:ALL' | (sudo EDITOR='tee -a' visudo) 
# Remove any existing system-installed Ansible to avoid version conflicts
sudo apt remove -y ansible || true
sudo pip3 uninstall -y ansible ansible-base ansible-core || true
# Install ansible-core compatible with Python 3.8 (Ubuntu 20.04)
python3 -m pip install --user "ansible-core>=2.12,<2.14"
# Ensure the pip user-installed ansible is in PATH and takes precedence
export PATH=/home/$1/.local/bin:$PATH
# Configure Docker to use the data disk at /mnt
sudo mkdir -p /mnt/docker
sudo systemctl stop docker || true
echo '{"data-root": "/mnt/docker"}' | sudo tee /etc/docker/daemon.json
sudo systemctl start docker
# Command to install sb dependencies.
python3 -m pip install .
# Command to build sb
make postinstall 
# This command initiates /dev/nvidiactl and /dev/nvidia-uvm directories, which sb checks before running.
sudo docker run --rm --gpus all nvidia/cuda:11.0.3-base nvidia-smi 