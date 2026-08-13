"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createElement, type ReactNode, useState } from "react";

export function QueryProvider({ children }: { children: ReactNode }) {
  const [queryClient] = useState(() => new QueryClient());

  return createElement(QueryClientProvider, { client: queryClient }, children);
}
