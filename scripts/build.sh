# Build script for Docker images
#!/bin/bash

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🐳 Building EventManagement Docker Image...${NC}"

# Set variables
IMAGE_NAME="eventmanagement-api"
IMAGE_TAG="latest"
REGISTRY=""

# Build Docker image
docker build -t $IMAGE_NAME:$IMAGE_TAG ./src/backend/EventManagement

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Docker image built successfully: $IMAGE_NAME:$IMAGE_TAG${NC}"
else
    echo -e "${RED}❌ Docker build failed${NC}"
    exit 1
fi

# Optional: Tag for Azure Container Registry
if [ ! -z "$1" ]; then
    REGISTRY=$1
    echo -e "${YELLOW}🏷️  Tagging for registry: $REGISTRY${NC}"
    docker tag $IMAGE_NAME:$IMAGE_TAG $REGISTRY/$IMAGE_NAME:$IMAGE_TAG
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Image tagged: $REGISTRY/$IMAGE_NAME:$IMAGE_TAG${NC}"
    else
        echo -e "${RED}❌ Tagging failed${NC}"
        exit 1
    fi
fi

echo -e "${GREEN}🎉 Build complete!${NC}"
echo -e "${YELLOW}💡 To run locally: docker-compose up${NC}"
echo -e "${YELLOW}💡 To push to Azure: docker push $REGISTRY/$IMAGE_NAME:$IMAGE_TAG${NC}"
