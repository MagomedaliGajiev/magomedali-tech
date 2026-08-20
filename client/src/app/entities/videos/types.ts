export type ChunkUploadUrl = {
  partNumber: number;
  uploadUrl: string;
};

export type StartMultipartUploadRequest = {
  fileName: string;
  assetType: "video";
  contentType: string;
  size: number;
};

export type StartMultipartUploadResponse = {
  mediaAssetId: string;
  uploadId: string;
  chunkUploadUrls: ChunkUploadUrl[];
  chunkSize: number;
};

export type PartETag = {
  partNumber: number;
  eTag: string;
};

export type CompleteMultipartUploadRequest = {
  mediaAssetId: string;
  uploadId: string;
  partETags: PartETag[];
};
