import clsx from "clsx";

export default function Card({ children, className }) {
  return (
    <div
      className={clsx("overflow-hidden rounded-lg bg-white shadow", className)}
    >
      <div className="px-4 py-5 sm:p-6">{children}</div>
    </div>
  );
}
