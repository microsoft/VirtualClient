import React from 'react';
import clsx from 'clsx';
import styles from './styles.module.css';

const FeatureList = [
  {
    title: 'Designed for Cloud Systems.',
    Svg: require('@site/static/img/undraw/undraw_connected_world_wuay.svg').default,
    description: (
      <>
        Supports a wide range of industry-standard benchmarks designed to thoroughly cover system performance, to reduce variance and increase confidence in measurements.
      </>
    ),
  },
  {
    title: 'Cross-Platform, Cross-Architecture. Comprehensive.',
    Svg: require('@site/static/img/undraw/undraw_in_sync_re_jlqd.svg').default,
    description: (
      <>
         Evaluates CPU, GPU, Memory, Storage, Network and more, on X64 and ARM64 architectures, Windows and Linux distributions.
      </>
    ),
  },
  {
    title: 'Deterministic Configurations Crafted by Experts.',
    Svg: require('@site/static/img/undraw/undraw_split_testing_l1uw.svg').default,
    description: (
      <>
        Workloads selected and configured by a community of experts, designed for exhaustive system analysis to produce data you can trust.
      </>
    ),
  },
];

function Feature({Svg, title, description}) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <Svg className={styles.featureSvg} role="img" />
      </div>
      <div className="text--center padding-horiz--md">
        <h3>{title}</h3>
        <p>{description}</p>
      </div>
    </div>
  );
}


const Index = () => {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
};

export default Index;