"use client";

import { FormEvent, useMemo, useState } from "react";
import {
  Check,
  Circle,
  ListChecks,
  MousePointerClick,
  Plus,
  Sparkles,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

type Todo = {
  id: number;
  text: string;
  completed: boolean;
  category: string;
};

type Filter = "all" | "active" | "completed";

const initialTodos: Todo[] = [
  {
    id: 1,
    text: "Подготовить структуру проекта",
    completed: false,
    category: "Разработка",
  },
  {
    id: 2,
    text: "Собрать интерфейс списка задач",
    completed: true,
    category: "Дизайн",
  },
  {
    id: 3,
    text: "Проверить адаптивность",
    completed: false,
    category: "Проверка",
  },
  {
    id: 4,
    text: "Написать тесты для компонентов",
    completed: false,
    category: "Качество",
  },
];

const filters: { value: Filter; label: string }[] = [
  { value: "all", label: "Все" },
  { value: "active", label: "В работе" },
  { value: "completed", label: "Готово" },
];

export default function Todo() {
  const [todos, setTodos] = useState(initialTodos);
  const [filter, setFilter] = useState<Filter>("all");
  const [newTodo, setNewTodo] = useState("");

  const completedCount = todos.filter((todo) => todo.completed).length;
  const progress = todos.length
    ? Math.round((completedCount / todos.length) * 100)
    : 0;
  const visibleTodos = useMemo(() => {
    if (filter === "active") return todos.filter((todo) => !todo.completed);
    if (filter === "completed") return todos.filter((todo) => todo.completed);
    return todos;
  }, [filter, todos]);

  function addTodo(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const text = newTodo.trim();

    if (!text) return;

    setTodos((currentTodos) => [
      ...currentTodos,
      {
        id: Math.max(0, ...currentTodos.map((todo) => todo.id)) + 1,
        text,
        completed: false,
        category: "Новое",
      },
    ]);
    setNewTodo("");
    setFilter("all");
  }

  function toggleTodo(id: number) {
    setTodos((currentTodos) =>
      currentTodos.map((todo) =>
        todo.id === id ? { ...todo, completed: !todo.completed } : todo,
      ),
    );
  }

  return (
    <section className="relative" aria-labelledby="todo-heading">
      <header className="flex items-start justify-between gap-5">
        <div>
          <div className="mb-3 flex items-center gap-2 text-[0.68rem] font-semibold uppercase tracking-[0.2em] text-primary">
            <Sparkles className="size-3.5" aria-hidden="true" />
            Фокус на сегодня
          </div>
          <h1
            id="todo-heading"
            className="text-2xl font-semibold tracking-[-0.035em] text-foreground sm:text-3xl"
          >
            План на сегодня
          </h1>
          <p className="mt-2 max-w-md text-sm leading-6 text-muted-foreground">
            Небольшие шаги каждый день складываются в большой результат.
          </p>
        </div>

        <div className="flex size-12 shrink-0 items-center justify-center rounded-full bg-primary text-white shadow-[0_10px_28px_rgba(255,0,51,0.2)] sm:size-14">
          <ListChecks className="size-6" aria-hidden="true" />
        </div>
      </header>

      <div className="mt-7 rounded-xl bg-[#181818] p-4 ring-1 ring-white/5 sm:p-5">
        <div className="flex items-end justify-between gap-4">
          <div>
            <p className="text-sm font-medium text-foreground">
              Дневной прогресс
            </p>
            <p className="mt-1 text-xs text-muted-foreground">
              {completedCount} из {todos.length} задач завершено
            </p>
          </div>
          <span className="text-2xl font-semibold tabular-nums tracking-tight text-primary">
            {progress}%
          </span>
        </div>
        <div
          className="mt-4 h-2.5 overflow-hidden rounded-full bg-muted"
          role="progressbar"
          aria-label="Прогресс выполнения задач"
          aria-valuemin={0}
          aria-valuemax={100}
          aria-valuenow={progress}
        >
          <div
            className="h-full rounded-full bg-primary transition-[width] duration-500"
            style={{ width: `${progress}%` }}
          />
        </div>
      </div>

      <form className="mt-4 flex gap-2" onSubmit={addTodo}>
        <Input
          value={newTodo}
          onChange={(event) => setNewTodo(event.target.value)}
          className="h-11 rounded-full border-[#303030] bg-[#121212] px-4 placeholder:text-muted-foreground/70 focus-visible:border-primary focus-visible:ring-primary/25"
          placeholder="Добавить новую задачу…"
          aria-label="Название новой задачи"
        />
        <Button
          type="submit"
          aria-label="Добавить задачу"
          className="h-11 gap-2 rounded-full bg-primary px-5 text-white shadow-sm hover:bg-[#d9002b]"
          disabled={!newTodo.trim()}
        >
          <Plus className="size-4" aria-hidden="true" />
          <span className="hidden sm:inline">Добавить</span>
        </Button>
      </form>

      <div className="mt-6 flex flex-wrap items-center justify-between gap-3">
        <div className="flex rounded-full bg-background/60 p-1">
          {filters.map((item) => (
            <button
              key={item.value}
              type="button"
              onClick={() => setFilter(item.value)}
              aria-pressed={filter === item.value}
              className={`rounded-full px-3 py-1.5 text-xs font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary ${
                filter === item.value
                  ? "bg-white text-black"
                  : "text-muted-foreground hover:bg-white/10 hover:text-foreground"
              }`}
            >
              {item.label}
            </button>
          ))}
        </div>
        <span className="text-xs font-medium text-muted-foreground">
          Осталось: {todos.length - completedCount}
        </span>
      </div>

      <ol className="mt-3 space-y-2.5" aria-live="polite">
        {visibleTodos.map((todo) => (
          <li key={todo.id}>
            <button
              type="button"
              onClick={() => toggleTodo(todo.id)}
              className={`group flex w-full items-center gap-3.5 rounded-xl p-3.5 text-left transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary sm:p-4 ${
                todo.completed
                  ? "bg-primary/[0.08] ring-1 ring-primary/20"
                  : "bg-[#181818] ring-1 ring-white/5 hover:bg-[#272727]"
              }`}
              aria-label={`${todo.completed ? "Вернуть в работу" : "Завершить"}: ${todo.text}`}
            >
              <span
                className={`flex size-9 shrink-0 items-center justify-center rounded-full border transition-colors ${
                  todo.completed
                    ? "border-primary bg-primary text-white"
                    : "border-muted-foreground/35 bg-muted/45 text-transparent group-hover:border-primary"
                }`}
              >
                {todo.completed ? (
                  <Check className="size-4" strokeWidth={3} aria-hidden="true" />
                ) : (
                  <Circle className="size-2 fill-current" aria-hidden="true" />
                )}
              </span>

              <span className="min-w-0 flex-1">
                <span
                  className={`block truncate text-sm font-medium transition-colors ${
                    todo.completed
                      ? "text-muted-foreground line-through decoration-primary/70"
                      : "text-foreground"
                  }`}
                >
                  {todo.text}
                </span>
                <span className="mt-1.5 flex items-center gap-2 text-[0.7rem] text-muted-foreground">
                  <span className="rounded-md bg-muted/70 px-1.5 py-0.5">
                    {todo.category}
                  </span>
                  <span>{todo.completed ? "Выполнено" : "Сегодня"}</span>
                </span>
              </span>

              <span className="hidden text-xs font-semibold tabular-nums text-muted-foreground/60 sm:block">
                {String(todo.id).padStart(2, "0")}
              </span>
            </button>
          </li>
        ))}
      </ol>

      {visibleTodos.length === 0 && (
        <div className="mt-3 rounded-2xl border border-dashed border-white/10 px-5 py-8 text-center text-sm text-muted-foreground">
          В этой категории пока нет задач.
        </div>
      )}

      <footer className="mt-5 flex items-center gap-2 text-[0.7rem] text-muted-foreground/75">
        <MousePointerClick className="size-3.5" aria-hidden="true" />
        Нажмите на задачу, чтобы изменить её статус
      </footer>
    </section>
  );
}
