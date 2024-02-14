# Metaseq apptainer image stpes
The following document will show steps followed in apptainer def file to curate apptainer image for metaseq.

Bootstrap: localimage
From: $BASE_APPTAINER_IMAGE
 
%files
 
%post
    mkdir -p /opt/metaseq_stack
    cd /opt/metaseq_stack
 
    pip3 install boto3
 
    # Install Fairscale
    git clone -b fixing_memory_issues_with_keeping_overlap_may24 https://github.com/facebookresearch/fairscale.git
    cd fairscale
    git checkout 91132c7e997c5affe97ce002e52cadd798220b06
    pip3 install -e .
    cd ..
 
    # Install metaseq
    git clone https://github.com/facebookresearch/metaseq.git
    cd metaseq
    git checkout 4de251ffca9a0717561f22bd721ee1ba7a8a5783
    pip3 install -e  .
    cd ..