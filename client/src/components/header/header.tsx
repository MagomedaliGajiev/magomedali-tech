import Link from "next/link";
import { Play } from "lucide-react";
import { routes } from "@/shared/routes";
import { SidebarTrigger } from "@/components/ui/sidebar";

export default function Header() {
  return (
    <header className="sticky top-0 z-50 border-b border-white/[0.06] bg-[#0f0f0f]/95 backdrop-blur-xl">
      <div className="flex h-16 w-full items-center px-4 sm:px-6">
        <SidebarTrigger
          className="mr-3 md:hidden"
          aria-label="Открыть меню"
        />
        <Link
          href={routes.home}
          className="group flex shrink-0 items-center gap-2 rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
          aria-label="Maraphon — главная"
        >
          <span className="flex h-7 w-10 items-center justify-center rounded-[0.45rem] bg-primary text-white shadow-[0_8px_24px_rgba(255,0,51,0.18)] transition-transform group-hover:scale-105">
            <Play
              className="size-4 translate-x-px fill-current"
              aria-hidden="true"
            />
          </span>
          <span className="hidden items-start sm:flex">
            <span className="text-xl font-bold leading-none tracking-[-0.055em]">
              Fullstack
            </span>
            <span className="ml-1 text-[0.55rem] leading-none text-muted-foreground">
              FS
            </span>
          </span>
        </Link>

        <div className="ml-auto">
          <div
            className="flex size-9 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-sky-500 to-blue-700 text-sm font-semibold text-white shadow-[0_0_0_2px_#0f0f0f,0_0_0_3px_rgba(255,255,255,0.16)]"
            aria-label="Профиль пользователя"
            title="Профиль"
          >
            M
          </div>
        </div>
      </div>
    </header>
  );
}
