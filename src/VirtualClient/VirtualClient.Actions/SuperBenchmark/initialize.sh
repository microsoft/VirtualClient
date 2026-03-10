# Ansible will use sudo which needs explicit password input. This command removes that step.
echo '$1 ALL=(ALL) NOPASSWD:ALL' | (sudo EDITOR='tee -a' visudo) 

# Remove any existing system-installed Ansible to avoid version conflicts
sudo apt remove -y ansible || true
sudo pip3 uninstall -y ansible ansible-base ansible-core || true

# Install ansible-core compatible with Python 3.8 (Ubuntu 20.04)
python3 -m pip install --user "ansible-core>=2.12,<2.14"

# Ensure the pip user-installed ansible is in PATH and takes precedence
export PATH=/home/$1/.local/bin:$PATH

# Configure Docker to use the data disk at path, unless not provided
if [[ -n "${2:-}" ]]; then
  DOCKER_DATA_ROOT="$2"
  echo "Configuring Docker data-root at ${DOCKER_DATA_ROOT} ..."

  # Create target path and stop Docker cleanly
  sudo mkdir -p "${DOCKER_DATA_ROOT}"
  sudo systemctl stop docker || true

  # Write/merge daemon.json to set data-root
  # If jq is present and an existing file exists, merge to preserve other keys; otherwise overwrite minimal file.
  if command -v jq >/dev/null 2>&1 && [[ -f /etc/docker/daemon.json ]]; then
    TMP_JSON=$(mktemp)
    sudo jq --arg dr "${DOCKER_DATA_ROOT}" '. + { "data-root": $dr }' /etc/docker/daemon.json | sudo tee "${TMP_JSON}" >/dev/null
    sudo mv "${TMP_JSON}" /etc/docker/daemon.json
  else
    echo "{\"data-root\": \"${DOCKER_DATA_ROOT}\"}" | sudo tee /etc/docker/daemon.json >/dev/null
  fi

  # Start Docker back up
  sudo systemctl start docker

  # (Optional) Warm-up/check NVIDIA devices as you had in the commented section
  # sudo docker run --rm --gpus all nvidia/cuda:11.0.3-base nvidia-smi
else
  echo "No second argument provided; skipping Docker data-root configuration."
fi

# Command to install sb dependencies.
python3 -m pip install .

# Command to build sb.
make postinstall 

# This command initiates /dev/nvidiactl and /dev/nvidia-uvm directories, which sb checks before running.
sudo docker run --rm --gpus all nvidia/cuda:11.0.3-base nvidia-smi 