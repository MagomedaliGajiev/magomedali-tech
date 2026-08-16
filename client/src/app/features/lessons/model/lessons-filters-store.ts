"use client";

import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";

export const LESSONS_SEARCH_MAX_LENGTH = 1000;
export const LESSON_PAGE_SIZES = [12, 24, 48] as const;

export type LessonPageSize = (typeof LESSON_PAGE_SIZES)[number];

type LessonsFiltersState = {
  search: string;
  isDeleted: boolean;
  pageSize: LessonPageSize;
};

type LessonsFiltersActions = {
  setSearch: (search: string) => void;
  setIsDeleted: (isDeleted: boolean) => void;
  setPageSize: (pageSize: LessonPageSize) => void;
  reset: () => void;
};

export type LessonsFiltersStore = LessonsFiltersState & LessonsFiltersActions;

export const DEFAULT_LESSONS_FILTERS: LessonsFiltersState = {
  search: "",
  isDeleted: false,
  pageSize: 12,
};

export const useLessonsFiltersStore = create<LessonsFiltersStore>()(
  persist(
    (set) => ({
      ...DEFAULT_LESSONS_FILTERS,
      setSearch: (search) =>
        set({ search: search.slice(0, LESSONS_SEARCH_MAX_LENGTH) }),
      setIsDeleted: (isDeleted) => set({ isDeleted }),
      setPageSize: (pageSize) => set({ pageSize }),
      reset: () => set(DEFAULT_LESSONS_FILTERS),
    }),
    {
      name: "lessons-filters",
      version: 1,
      storage: createJSONStorage(() => localStorage),
      partialize: ({ search, isDeleted, pageSize }) => ({
        search,
        isDeleted,
        pageSize,
      }),
      merge: (persistedState, currentState) => {
        const persisted =
          typeof persistedState === "object" && persistedState !== null
            ? (persistedState as Partial<LessonsFiltersState>)
            : {};
        const persistedPageSize = persisted.pageSize as LessonPageSize;

        return {
          ...currentState,
          search:
            typeof persisted.search === "string"
              ? persisted.search.slice(0, LESSONS_SEARCH_MAX_LENGTH)
              : DEFAULT_LESSONS_FILTERS.search,
          isDeleted:
            typeof persisted.isDeleted === "boolean"
              ? persisted.isDeleted
              : DEFAULT_LESSONS_FILTERS.isDeleted,
          pageSize: LESSON_PAGE_SIZES.includes(persistedPageSize)
            ? persistedPageSize
            : DEFAULT_LESSONS_FILTERS.pageSize,
        };
      },
      skipHydration: true,
    },
  ),
);
