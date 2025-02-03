import { Popover, ArrowContainer } from "react-tiny-popover";
import Markdown from "react-markdown";
import TypeTooltip from "./TypeTooltip";

export default function Tooltip({
  children,
  showToolTip,
  text,
  types,
  positions,
}) {
  return (
    <Popover
      isOpen={showToolTip}
      positions={positions} // preferred positions by priority
      content={({ position, childRect, popoverRect }) => (
        <ArrowContainer // if you'd like an arrow, you can import the ArrowContainer!
          position={position}
          childRect={childRect}
          popoverRect={popoverRect}
          arrowColor="#1e2939"
          padding={2}
          arrowSize={10}
          arrowStyle={{}}
          className="popover-arrow-container"
          arrowClassName="popover-arrow"
        >
          <div
            style={{ zIndex: 1000 }}
            className="flex flex-col items-center gap-2 rounded-md bg-gray-800 p-2 text-sm text-white"
          >
            <Markdown>{text}</Markdown>
            {types && <TypeTooltip types={types} />}
          </div>
        </ArrowContainer>
      )}
    >
      {children}
    </Popover>
  );
}
