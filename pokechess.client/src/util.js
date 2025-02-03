import clsx from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs) {
  return twMerge(clsx(inputs));
}

export function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export function getElementPosition(id) {
  const element = document?.getElementById(id);
  const rect = element?.getBoundingClientRect();

  if (!rect) {
    throw new Error("Could not find ID: " + id);
  }

  return [rect?.top, rect?.left];
}
