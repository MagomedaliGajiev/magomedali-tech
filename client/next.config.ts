import type { NextConfig } from "next";

const apiInternalUrl = process.env.API_INTERNAL_URL ?? "http://localhost:8001";
const fileApiInternalUrl =
  process.env.FILE_API_INTERNAL_URL ?? "http://localhost:8002";

const nextConfig: NextConfig = {
  reactCompiler: true,
  allowedDevOrigins: ["192.168.56.1"],
  async rewrites() {
    return [
      {
        source: "/api/files/:path*",
        destination: `${fileApiInternalUrl}/api/files/:path*`,
      },
      {
        source: "/api/:path*",
        destination: `${apiInternalUrl}/:path*`,
      },
    ];
  },
};

export default nextConfig;
