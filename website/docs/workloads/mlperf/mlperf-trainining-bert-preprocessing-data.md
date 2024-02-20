# MLPerf Trainging Bert Preprocessing Data
The following document will show steps ran for Downloading,Preprocessing and Packaging the training data used in Bert training.

## VM Configuration used
  * VMSKU: Standard ND96asr v4 (96 vcpus, 900 GiB memory)
  * Operating System : Ubuntu 20.04
  * OS Disk Size: 256 GB
  * Dats Disk Size : 8TB (Mounted on `/data/mlperf/bert`)

## Steps followed to preprocess data
  * Git cloned [MLPerfTraining](https://github.com/mlcommons/training_results_v2.1)
  
  * Visit [Bert Benchmark](https://github.com/mlcommons/training_results_v2.1/tree/main/NVIDIA/benchmarks/bert/implementations/pytorch-22.09)
  
  * Update "[requirements.txt](https://github.com/mlcommons/training_results_v2.1/blob/main/NVIDIA/benchmarks/bert/implementations/pytorch-22.09/requirements.txt)" gdown section
    "gdown==4.4.0" to "gdown==4.7.1" (Current Latest)
    To avoid failure of download from Google Drive.
  
  * Run following commands:
  ```bash
  docker build --pull -t mlperf-training:language_model .

  docker push mlperf-training:language_model

  docker run --runtime=nvidia --ipc=host -v /data/mlperf/bert:/workspace/bert_data mlperf-training:language_model
  ```
  
  * Inside docker container run following commands:
  ```bash
  cd /workspace/bert
  ./input_preprocessing/prepare_data.sh --outputdir /workspace/bert_data
  ```
  
  * Inside container follow steps to package data [link](https://github.com/mlcommons/training_results_v2.1/tree/main/NVIDIA/benchmarks/bert/implementations/pytorch-22.09/input_preprocessing/packed_data)

  * Exit container and zip `/data/mlperf/bert`
