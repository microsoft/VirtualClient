# Ansible will use sudo which needs explicit password input. This command removes that step.
echo '$1 ALL=(ALL) NOPASSWD:ALL' | (sudo EDITOR='tee -a' visudo) 
# sb binary might be in this path. This command adds this path to the PATH variable.
export PATH=$PATH:/home/$1/.local/bin
# Command to install sb dependencies.
python3 -m pip install .
# Command to build sb.
make postinstall 
# This command initiates /dev/nvidiactl and /dev/nvidia-uvm directories, which sb checks before running.
sudo docker run --rm --gpus all nvidia/cuda:11.0.3-base nvidia-smi 