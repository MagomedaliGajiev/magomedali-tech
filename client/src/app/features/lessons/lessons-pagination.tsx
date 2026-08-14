"use client";

import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/shared/components/ui/pagination";

type PaginationEntry = number | "start-ellipsis" | "end-ellipsis";

const getPaginationEntries = (
  currentPage: number,
  totalPages: number,
): PaginationEntry[] => {
  if (totalPages <= 7) {
    return Array.from({ length: totalPages }, (_, index) => index + 1);
  }

  if (currentPage <= 4) {
    return [1, 2, 3, 4, 5, "end-ellipsis", totalPages];
  }

  if (currentPage >= totalPages - 3) {
    return [
      1,
      "start-ellipsis",
      totalPages - 4,
      totalPages - 3,
      totalPages - 2,
      totalPages - 1,
      totalPages,
    ];
  }

  return [
    1,
    "start-ellipsis",
    currentPage - 1,
    currentPage,
    currentPage + 1,
    "end-ellipsis",
    totalPages,
  ];
};

type LessonsPaginationProps = {
  currentPage: number;
  totalPages: number;
  isFetching: boolean;
  onPageChange: (page: number) => void;
};

export function LessonsPagination({
  currentPage,
  totalPages,
  isFetching,
  onPageChange,
}: LessonsPaginationProps) {
  if (totalPages <= 1) {
    return null;
  }

  const paginationEntries = getPaginationEntries(currentPage, totalPages);
  const isPreviousDisabled = currentPage === 1 || isFetching;
  const isNextDisabled = currentPage >= totalPages || isFetching;

  const goToPage = (page: number) => {
    if (isFetching || page === currentPage || page < 1 || page > totalPages) {
      return;
    }

    onPageChange(page);
  };

  return (
    <Pagination className="mt-6">
      <PaginationContent>
        <PaginationItem>
          <PaginationPrevious
            href="#lessons-list"
            text="Назад"
            aria-label="Перейти на предыдущую страницу"
            aria-disabled={isPreviousDisabled}
            tabIndex={isPreviousDisabled ? -1 : undefined}
            className={
              isPreviousDisabled ? "pointer-events-none opacity-50" : undefined
            }
            onClick={(event) => {
              event.preventDefault();
              goToPage(currentPage - 1);
            }}
          />
        </PaginationItem>

        {paginationEntries.map((entry) => (
          <PaginationItem key={entry}>
            {typeof entry === "number" ? (
              <PaginationLink
                href="#lessons-list"
                isActive={entry === currentPage}
                aria-label={`Перейти на страницу ${entry}`}
                onClick={(event) => {
                  event.preventDefault();
                  goToPage(entry);
                }}
              >
                {entry}
              </PaginationLink>
            ) : (
              <PaginationEllipsis />
            )}
          </PaginationItem>
        ))}

        <PaginationItem>
          <PaginationNext
            href="#lessons-list"
            text="Вперёд"
            aria-label="Перейти на следующую страницу"
            aria-disabled={isNextDisabled}
            tabIndex={isNextDisabled ? -1 : undefined}
            className={
              isNextDisabled ? "pointer-events-none opacity-50" : undefined
            }
            onClick={(event) => {
              event.preventDefault();
              goToPage(currentPage + 1);
            }}
          />
        </PaginationItem>
      </PaginationContent>
    </Pagination>
  );
}
