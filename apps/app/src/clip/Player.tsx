import { useRef, useState } from "react";

// Plays a clip from its signed URL, which expires (urlsExpireAt). Once it has, seeking fails, so the player asks for
// a new URL, and a new URL carries on from where the old one was instead of starting over.
export function Player({
  src,
  expiresAt,
  onExpired,
}: {
  src: string;
  expiresAt: string;
  onExpired: () => void;
}) {
  const position = useRef({ time: 0, playing: false });
  const [failed, setFailed] = useState(false);

  const remember = (video: HTMLVideoElement) => {
    position.current = { time: video.currentTime, playing: !video.paused };
  };

  return (
    <div className="flex flex-col gap-3">
      <video
        src={src}
        controls
        playsInline
        preload="metadata"
        onTimeUpdate={(e) => remember(e.currentTarget)}
        onPause={(e) => remember(e.currentTarget)}
        onLoadedMetadata={(e) => {
          setFailed(false);
          const { time, playing } = position.current;
          if (time > 0) {
            e.currentTarget.currentTime = time;
            if (playing) void e.currentTarget.play();
          }
        }}
        onError={() => {
          // Only an expired URL is worth replacing: another error (a format the browser can't play) would come back
          // with the new URL too, and around again.
          if (Date.now() >= new Date(expiresAt).getTime()) onExpired();
          else setFailed(true);
        }}
        className="aspect-video w-full rounded-[14px] bg-black"
      />
      {failed && (
        <p role="alert" className="text-sm text-danger">
          Couldn't play this clip. The file may be missing, or in a format this browser can't play.
        </p>
      )}
    </div>
  );
}
