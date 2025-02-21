import { cn } from "../util";
export default function Button({ children, className, disabled, ...props }) {
  return (
    <button
      {...props}
      disabled={disabled}
      type="button"
      className={cn(
        "group inline-flex h-8 items-center justify-center overflow-hidden rounded-md border border-neutral-600 bg-amber-200 px-1 text-sm [box-shadow:0px_4px_1px_#a3a3a3]",
        disabled && "cursor-not-allowed opacity-50",
        !disabled &&
          "cursor-pointer transition-all active:translate-y-[2px] active:shadow-none",
        className,
      )}
    >
      {children}
    </button>
  );
}
