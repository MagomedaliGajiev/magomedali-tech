"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createElement, type ReactNode, useState } from "react";

const SECOND = 1_000;
const MINUTE = 60 * SECOND;

export function QueryProvider({ children }: { children: ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 30 * SECOND,
            gcTime: 10 * MINUTE,
            retry: 1,
            refetchOnWindowFocus: true,
            refetchOnReconnect: true,
          },
        },
      }),
  );

  return createElement(QueryClientProvider, { client: queryClient }, children);
}
