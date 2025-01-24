import clsx from "clsx";

export default function Row({ children, className }) {
  return (
    <div
      className={clsx(
        "flex h-1/5 w-screen items-center justify-center gap-3",
        className,
      )}
    >
      {children}
    </div>
  );
}
