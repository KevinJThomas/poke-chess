import uniqolor from "uniqolor";
import clsx from "clsx";

export default function Avatar({ name, className }) {
  function getInitials(name) {
    // Split the name into words based on spaces
    const words = name.split(" ");

    // Extract the first letter of each word and capitalize it
    const initials = words
      .map((word) => word.charAt(0).toUpperCase())
      .slice(0, 2);

    // Join the initials together
    return initials.join("");
  }

  const initials = getInitials(name);

  return (
    <span
      style={{ backgroundColor: uniqolor(name).color }}
      className={clsx(
        "mr-3 inline-flex size-8 shrink-0 items-center justify-center rounded-full",
        className,
      )}
    >
      <span className="text-sm font-medium text-white">{initials}</span>
    </span>
  );
}
