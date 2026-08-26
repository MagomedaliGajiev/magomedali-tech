export const AssetType = {
  VIDEO: "video",
  PREVIEW: "preview",
} as const;

export type AssetType = (typeof AssetType)[keyof typeof AssetType];

export const OwnerType = {
  LESSON: "lesson",
  MODULE: "module",
  USER: "user",
} as const;

export type OwnerType = (typeof OwnerType)[keyof typeof OwnerType];

export type MediaOwner = {
  ownerType: OwnerType;
  ownerId: string;
};

export type ChunkUploadUrl = {
  partNumber: number;
  uploadUrl: string;
};

export type StartMultipartUploadRequest = MediaOwner & {
  fileName: string;
  assetType: AssetType;
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
