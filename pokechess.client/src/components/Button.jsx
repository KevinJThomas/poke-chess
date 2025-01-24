import clsx from "clsx";
export default function Button({ children, className, ...props }) {
  return (
    <button
      {...props}
      type="button"
      className={clsx(
        "w-16 rounded bg-white px-2 py-1 text-xs font-semibold text-gray-900 ring-1 shadow-sm ring-gray-300 ring-inset hover:bg-gray-50",
        className,
      )}
    >
      {children}
    </button>
  );
}
